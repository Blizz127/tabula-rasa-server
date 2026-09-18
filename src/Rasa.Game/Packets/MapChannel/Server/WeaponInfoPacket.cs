namespace Rasa.Packets.MapChannel.Server
{
    using Data;
    using Memory;
    using Structures;

    public class WeaponInfoPacket : ServerPythonPacket
    {
        public override GameOpcode Opcode { get; } = GameOpcode.WeaponInfo;

        private readonly uint _clipSize, _currentAmmo, _reloadTime, _altActionId, _altActionArgId;
        private readonly uint _aeType, _aeRadius, _recoilAmount, _coolRate, _ammoPerShot;
        private readonly double _aimRate, _heatPerShot;
        private readonly int _toolType, _cameraProfile;
        private readonly bool _isJammed;

        public WeaponInfoPacket(Item item, EntityClass classInfo)
        {
            // WeaponInfo is queued alongside later ammo/jam updates. Retain the
            // instance and template values observed at this point in the stream.
            var weapon = item.ItemTemplate.WeaponInfo;
            _clipSize = classInfo.WeaponClassInfo.ClipSize;
            _currentAmmo = item.CurrentAmmo;
            _aimRate = weapon.AimRate;
            _reloadTime = weapon.ReloadTime;
            _altActionId = weapon.AltActionId;
            _altActionArgId = weapon.AltActionArgId;
            _aeType = weapon.AeType;
            _aeRadius = weapon.AeRadius;
            _recoilAmount = weapon.RecoilAmount;
            _coolRate = weapon.CoolRate;
            _heatPerShot = weapon.HeatPerShot;
            _toolType = (int)weapon.ToolType;
            _isJammed = item.IsJammed;
            _ammoPerShot = weapon.AmmoPerShot;
            _cameraProfile = item.CammeraProfile;
        }
        public override void Write(PythonWriter pw)
        {
            pw.WriteTuple(17);
            pw.WriteNoneStruct();       // maybe not used by client
            pw.WriteUInt(_clipSize);
            pw.WriteUInt(_currentAmmo);
            pw.WriteDouble(_aimRate);
            pw.WriteUInt(_reloadTime);
            pw.WriteUInt(_altActionId);
            pw.WriteUInt(_altActionArgId);
            // aetypes starts at 1; 0 means none. basetoolaction.py treats a non-None aeType as
            // TARGET_NONE, so writing 0 made every tool an area tool that could not be aimed.
            if (_aeType == 0)
                pw.WriteNoneStruct();
            else
                pw.WriteUInt(_aeType);
            pw.WriteUInt(_aeRadius);
            pw.WriteUInt(_recoilAmount);
            pw.WriteNoneStruct();       // ReuseOverride ToDo
            pw.WriteUInt(_coolRate);
            pw.WriteDouble(_heatPerShot);
            pw.WriteInt(_toolType);
            pw.WriteBool(_isJammed);
            pw.WriteUInt(_ammoPerShot);
            pw.WriteInt(_cameraProfile);
        }
    }
}
