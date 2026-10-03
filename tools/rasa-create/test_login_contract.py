"""Exercise a reviewed create-service candidate without importing or starting it.

Usage: python3 tools/rasa-create/test_login_contract.py /path/to/app.candidate.py
Only selected functions/constants are compiled; all account writes use a temporary
SQLite fixture. No production credentials, requests or service startup are used.
"""

import ast
import asyncio
import contextlib
import hashlib
import io
import os
from pathlib import Path
import re
import sqlite3
import sys
import tempfile
from types import SimpleNamespace
import unittest


SOURCE = Path(sys.argv.pop(1))


class ContractTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.db = Path(self.temp.name) / "fixture.db"
        with sqlite3.connect(self.db) as db:
            db.execute("CREATE TABLE account (id INTEGER PRIMARY KEY, email TEXT, "
                       "username TEXT, password TEXT, salt TEXT, level INTEGER, "
                       "locked INTEGER, validated INTEGER)")
        self.g = dict(re=re, sqlite3=sqlite3, os=os, AUTH_DB=self.db,
                      Request=object, CreateJsonBody=object,
                      Form=lambda *a: None,
                      JSONResponse=lambda **kw: SimpleNamespace(**kw),
                      _client_ip=lambda request: "fixture", _rate_ok=lambda ip: True)
        tree = ast.parse(SOURCE.read_text())
        names = {"_validate", "_find_account", "_hash_password", "_new_salt",
                 "_upsert_account", "create_account", "create_account_json"}
        nodes = []
        for node in tree.body:
            if isinstance(node, ast.Assign) and any(
                    isinstance(t, ast.Name) and t.id in {"EMAIL_RE", "USER_RE", "PASS_RE"}
                    for t in node.targets):
                nodes.append(node)
            elif isinstance(node, (ast.FunctionDef, ast.AsyncFunctionDef)) and node.name in names:
                node.decorator_list = []
                nodes.append(node)
        exec(compile(ast.Module(body=nodes, type_ignores=[]), str(SOURCE), "exec"), self.g)

    def call(self, route, password, confirm=None, username="fixture"):
        confirm = password if confirm is None else confirm
        with contextlib.redirect_stdout(io.StringIO()):
            if route == "json":
                body = SimpleNamespace(email="fixture@example.invalid", username=username,
                                       password=password, confirm=confirm)
                return asyncio.run(self.g["create_account_json"](object(), body))
            return asyncio.run(self.g["create_account"](
                object(), "fixture@example.invalid", username, password, confirm))

    def rows(self):
        with sqlite3.connect(self.db) as db:
            return db.execute("SELECT * FROM account ORDER BY id").fetchall()

    def test_rejects_overlong_password_before_insert_and_update(self):
        for route in ("json", "form"):
            for present in (False, True):
                with sqlite3.connect(self.db) as db:
                    db.execute("DELETE FROM account")
                if present:
                    self.call(route, "Fixture9")
                before = self.rows()
                result = self.call(route, "a" * 17)
                self.assertEqual(getattr(result, "status_code", 200), 400)
                self.assertEqual(self.rows(), before)

    def test_password_limits_and_hash_match_protocol_bytes(self):
        for route in ("json", "form"):
            for length in (6, 16):
                password = "A" * (length - 1) + "9"
                result = self.call(route, password)
                self.assertTrue(result["ok"])
                row = self.rows()[0]
                protocol_password = password.encode("utf-8")[:16].decode("utf-8")
                digest = hashlib.sha256((row[4] + ":" + protocol_password).encode()).hexdigest()
                self.assertEqual(row[3], digest)

    def test_whitespace_and_controls_are_rejected_without_normalization(self):
        for route in ("json", "form"):
            self.call(route, "Fixture9")
            before = self.rows()
            for password in (" Fixture9", "Fixture9 ", "Fix ture9", "Fix\tture9",
                             "Fixture9\n", "Fix\x00ture9"):
                result = self.call(route, password)
                self.assertEqual(getattr(result, "status_code", 200), 400)
                self.assertEqual(self.rows(), before)

    def test_confirmation_must_match_exactly(self):
        for route in ("json", "form"):
            result = self.call(route, "Fixture9", "Fixture9 ")
            self.assertEqual(getattr(result, "status_code", 200), 400)
            self.assertEqual(self.rows(), [])

    def test_username_packet_limit(self):
        for route in ("json", "form"):
            for username in ("a" * 15, "a" * 32, "a" * 13 + "é"):
                result = self.call(route, "Fixture9", username=username)
                self.assertEqual(getattr(result, "status_code", 200), 400)
                self.assertEqual(self.rows(), [])
            self.assertTrue(self.call(route, "Fixture9", username="a" * 14)["ok"])
            with sqlite3.connect(self.db) as db:
                db.execute("DELETE FROM account")

    def test_existing_password_policy_is_preserved(self):
        for route in ("json", "form"):
            for password in ("short", "Fïxture9", "Fixture$9", "Fixture'9"):
                result = self.call(route, password)
                self.assertEqual(getattr(result, "status_code", 200), 400)
                self.assertEqual(self.rows(), [])


if __name__ == "__main__":
    unittest.main()
