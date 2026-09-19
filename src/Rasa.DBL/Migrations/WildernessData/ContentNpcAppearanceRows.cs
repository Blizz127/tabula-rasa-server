using Microsoft.EntityFrameworkCore.Migrations;

namespace Rasa.Migrations.WildernessData
{
    /// <summary>
    /// Every NPC the content batches created was bare: "captain youngblood is naked and headless" (2026-09-19).
    ///
    /// They are all <c>NPC_Human_Swapset_Male</c> (entity class 3846), a body assembled from clothing pieces, and a
    /// creature with no <c>creature_appearance</c> rows gets none of them - no head either. BootcampFixNpcAppearance
    /// found this for the four boot-camp officers and gave them Outpost Commander Rogers' set (creature 100 in the
    /// shipped world data) as an analogue under OD-11; the fifty NPCs created since were left bare.
    ///
    /// Each now carries a shipped set of the same kind, an analogue under OD-11 and OD-45 (no per-NPC appearance
    /// source exists): Rogers' officer set (clothing 4021/4022/4023, hair 3672, face 24019, holstered pistol 27120),
    /// and for the doctors the set Dr. Munson and Dr. Soji wear (3941/3942/3943). None is claimed to be the
    /// original; a per-NPC source would replace them. See GAP-NPC-BODY.
    /// </summary>
    public static class ContentNpcAppearanceRows
    {
        public const string Migration = "ContentNpcAppearance";

        // (creatureId, slotId, classId, color)
        private static readonly uint[][] Rows =
        {
            new[] { 198505u, 3u, 4021u, 22120u },   // Captain Youngblood
            new[] { 198505u, 13u, 27120u, 1u },   // Captain Youngblood
            new[] { 198505u, 14u, 3672u, 14404004u },   // Captain Youngblood
            new[] { 198505u, 15u, 4022u, 933202u },   // Captain Youngblood
            new[] { 198505u, 16u, 4023u, 13933202u },   // Captain Youngblood
            new[] { 198505u, 17u, 24019u, 4286886614u },   // Captain Youngblood
            new[] { 198508u, 3u, 4021u, 22120u },   // Corporal Van Valkenberg
            new[] { 198508u, 13u, 27120u, 1u },   // Corporal Van Valkenberg
            new[] { 198508u, 14u, 3672u, 14404004u },   // Corporal Van Valkenberg
            new[] { 198508u, 15u, 4022u, 933202u },   // Corporal Van Valkenberg
            new[] { 198508u, 16u, 4023u, 13933202u },   // Corporal Van Valkenberg
            new[] { 198508u, 17u, 24019u, 4286886614u },   // Corporal Van Valkenberg
            new[] { 198509u, 3u, 4021u, 22120u },   // wounded AFS soldier (package 2584)
            new[] { 198509u, 13u, 27120u, 1u },   // wounded AFS soldier (package 2584)
            new[] { 198509u, 14u, 3672u, 14404004u },   // wounded AFS soldier (package 2584)
            new[] { 198509u, 15u, 4022u, 933202u },   // wounded AFS soldier (package 2584)
            new[] { 198509u, 16u, 4023u, 13933202u },   // wounded AFS soldier (package 2584)
            new[] { 198509u, 17u, 24019u, 4286886614u },   // wounded AFS soldier (package 2584)
            new[] { 198514u, 3u, 4021u, 22120u },   // Outpost Commander Rogers
            new[] { 198514u, 13u, 27120u, 1u },   // Outpost Commander Rogers
            new[] { 198514u, 14u, 3672u, 14404004u },   // Outpost Commander Rogers
            new[] { 198514u, 15u, 4022u, 933202u },   // Outpost Commander Rogers
            new[] { 198514u, 16u, 4023u, 13933202u },   // Outpost Commander Rogers
            new[] { 198514u, 17u, 24019u, 4286886614u },   // Outpost Commander Rogers
            new[] { 198515u, 3u, 4021u, 22120u },   // Training Officer Kincaid
            new[] { 198515u, 13u, 27120u, 1u },   // Training Officer Kincaid
            new[] { 198515u, 14u, 3672u, 14404004u },   // Training Officer Kincaid
            new[] { 198515u, 15u, 4022u, 933202u },   // Training Officer Kincaid
            new[] { 198515u, 16u, 4023u, 13933202u },   // Training Officer Kincaid
            new[] { 198515u, 17u, 24019u, 4286886614u },   // Training Officer Kincaid
            new[] { 199000u, 3u, 4021u, 22120u },   // Lt. Sebastian
            new[] { 199000u, 13u, 27120u, 1u },   // Lt. Sebastian
            new[] { 199000u, 14u, 3672u, 14404004u },   // Lt. Sebastian
            new[] { 199000u, 15u, 4022u, 933202u },   // Lt. Sebastian
            new[] { 199000u, 16u, 4023u, 13933202u },   // Lt. Sebastian
            new[] { 199000u, 17u, 24019u, 4286886614u },   // Lt. Sebastian
            new[] { 199002u, 2u, 3941u, 12429948u },   // Field Dr. Dawson [doctor]
            new[] { 199002u, 14u, 3672u, 1440400u },   // Field Dr. Dawson [doctor]
            new[] { 199002u, 15u, 3942u, 12429948u },   // Field Dr. Dawson [doctor]
            new[] { 199002u, 16u, 3943u, 12429948u },   // Field Dr. Dawson [doctor]
            new[] { 199002u, 17u, 24019u, 4286886614u },   // Field Dr. Dawson [doctor]
            new[] { 199003u, 3u, 4021u, 22120u },   // Receptive Liaison Brice
            new[] { 199003u, 13u, 27120u, 1u },   // Receptive Liaison Brice
            new[] { 199003u, 14u, 3672u, 14404004u },   // Receptive Liaison Brice
            new[] { 199003u, 15u, 4022u, 933202u },   // Receptive Liaison Brice
            new[] { 199003u, 16u, 4023u, 13933202u },   // Receptive Liaison Brice
            new[] { 199003u, 17u, 24019u, 4286886614u },   // Receptive Liaison Brice
            new[] { 199004u, 3u, 4021u, 22120u },   // Receptive Liaison Noonan
            new[] { 199004u, 13u, 27120u, 1u },   // Receptive Liaison Noonan
            new[] { 199004u, 14u, 3672u, 14404004u },   // Receptive Liaison Noonan
            new[] { 199004u, 15u, 4022u, 933202u },   // Receptive Liaison Noonan
            new[] { 199004u, 16u, 4023u, 13933202u },   // Receptive Liaison Noonan
            new[] { 199004u, 17u, 24019u, 4286886614u },   // Receptive Liaison Noonan
            new[] { 199005u, 3u, 4021u, 22120u },   // Agent Franz
            new[] { 199005u, 13u, 27120u, 1u },   // Agent Franz
            new[] { 199005u, 14u, 3672u, 14404004u },   // Agent Franz
            new[] { 199005u, 15u, 4022u, 933202u },   // Agent Franz
            new[] { 199005u, 16u, 4023u, 13933202u },   // Agent Franz
            new[] { 199005u, 17u, 24019u, 4286886614u },   // Agent Franz
            new[] { 199100u, 3u, 4021u, 22120u },   // Ranger Kogari
            new[] { 199100u, 13u, 27120u, 1u },   // Ranger Kogari
            new[] { 199100u, 14u, 3672u, 14404004u },   // Ranger Kogari
            new[] { 199100u, 15u, 4022u, 933202u },   // Ranger Kogari
            new[] { 199100u, 16u, 4023u, 13933202u },   // Ranger Kogari
            new[] { 199100u, 17u, 24019u, 4286886614u },   // Ranger Kogari
            new[] { 199101u, 3u, 4021u, 22120u },   // Ranger Urialia
            new[] { 199101u, 13u, 27120u, 1u },   // Ranger Urialia
            new[] { 199101u, 14u, 3672u, 14404004u },   // Ranger Urialia
            new[] { 199101u, 15u, 4022u, 933202u },   // Ranger Urialia
            new[] { 199101u, 16u, 4023u, 13933202u },   // Ranger Urialia
            new[] { 199101u, 17u, 24019u, 4286886614u },   // Ranger Urialia
            new[] { 199102u, 3u, 4021u, 22120u },   // Warden Brocail
            new[] { 199102u, 13u, 27120u, 1u },   // Warden Brocail
            new[] { 199102u, 14u, 3672u, 14404004u },   // Warden Brocail
            new[] { 199102u, 15u, 4022u, 933202u },   // Warden Brocail
            new[] { 199102u, 16u, 4023u, 13933202u },   // Warden Brocail
            new[] { 199102u, 17u, 24019u, 4286886614u },   // Warden Brocail
            new[] { 199103u, 3u, 4021u, 22120u },   // Warden Lagori
            new[] { 199103u, 13u, 27120u, 1u },   // Warden Lagori
            new[] { 199103u, 14u, 3672u, 14404004u },   // Warden Lagori
            new[] { 199103u, 15u, 4022u, 933202u },   // Warden Lagori
            new[] { 199103u, 16u, 4023u, 13933202u },   // Warden Lagori
            new[] { 199103u, 17u, 24019u, 4286886614u },   // Warden Lagori
            new[] { 199104u, 3u, 4021u, 22120u },   // Warden Kahlee
            new[] { 199104u, 13u, 27120u, 1u },   // Warden Kahlee
            new[] { 199104u, 14u, 3672u, 14404004u },   // Warden Kahlee
            new[] { 199104u, 15u, 4022u, 933202u },   // Warden Kahlee
            new[] { 199104u, 16u, 4023u, 13933202u },   // Warden Kahlee
            new[] { 199104u, 17u, 24019u, 4286886614u },   // Warden Kahlee
            new[] { 199105u, 3u, 4021u, 22120u },   // Field Lt. Brody
            new[] { 199105u, 13u, 27120u, 1u },   // Field Lt. Brody
            new[] { 199105u, 14u, 3672u, 14404004u },   // Field Lt. Brody
            new[] { 199105u, 15u, 4022u, 933202u },   // Field Lt. Brody
            new[] { 199105u, 16u, 4023u, 13933202u },   // Field Lt. Brody
            new[] { 199105u, 17u, 24019u, 4286886614u },   // Field Lt. Brody
            new[] { 199106u, 3u, 4021u, 22120u },   // Field Lt. Bagby
            new[] { 199106u, 13u, 27120u, 1u },   // Field Lt. Bagby
            new[] { 199106u, 14u, 3672u, 14404004u },   // Field Lt. Bagby
            new[] { 199106u, 15u, 4022u, 933202u },   // Field Lt. Bagby
            new[] { 199106u, 16u, 4023u, 13933202u },   // Field Lt. Bagby
            new[] { 199106u, 17u, 24019u, 4286886614u },   // Field Lt. Bagby
            new[] { 199107u, 3u, 4021u, 22120u },   // Lt. Galloway
            new[] { 199107u, 13u, 27120u, 1u },   // Lt. Galloway
            new[] { 199107u, 14u, 3672u, 14404004u },   // Lt. Galloway
            new[] { 199107u, 15u, 4022u, 933202u },   // Lt. Galloway
            new[] { 199107u, 16u, 4023u, 13933202u },   // Lt. Galloway
            new[] { 199107u, 17u, 24019u, 4286886614u },   // Lt. Galloway
            new[] { 199108u, 3u, 4021u, 22120u },   // Captain Fransisco (Devil's Den)
            new[] { 199108u, 13u, 27120u, 1u },   // Captain Fransisco (Devil's Den)
            new[] { 199108u, 14u, 3672u, 14404004u },   // Captain Fransisco (Devil's Den)
            new[] { 199108u, 15u, 4022u, 933202u },   // Captain Fransisco (Devil's Den)
            new[] { 199108u, 16u, 4023u, 13933202u },   // Captain Fransisco (Devil's Den)
            new[] { 199108u, 17u, 24019u, 4286886614u },   // Captain Fransisco (Devil's Den)
            new[] { 199200u, 3u, 4021u, 22120u },   // Field Lt. Peterson
            new[] { 199200u, 13u, 27120u, 1u },   // Field Lt. Peterson
            new[] { 199200u, 14u, 3672u, 14404004u },   // Field Lt. Peterson
            new[] { 199200u, 15u, 4022u, 933202u },   // Field Lt. Peterson
            new[] { 199200u, 16u, 4023u, 13933202u },   // Field Lt. Peterson
            new[] { 199200u, 17u, 24019u, 4286886614u },   // Field Lt. Peterson
            new[] { 199201u, 3u, 4021u, 22120u },   // Field Sgt. Garde
            new[] { 199201u, 13u, 27120u, 1u },   // Field Sgt. Garde
            new[] { 199201u, 14u, 3672u, 14404004u },   // Field Sgt. Garde
            new[] { 199201u, 15u, 4022u, 933202u },   // Field Sgt. Garde
            new[] { 199201u, 16u, 4023u, 13933202u },   // Field Sgt. Garde
            new[] { 199201u, 17u, 24019u, 4286886614u },   // Field Sgt. Garde
            new[] { 199202u, 3u, 4021u, 22120u },   // Alpha Squad Commander Carvelle
            new[] { 199202u, 13u, 27120u, 1u },   // Alpha Squad Commander Carvelle
            new[] { 199202u, 14u, 3672u, 14404004u },   // Alpha Squad Commander Carvelle
            new[] { 199202u, 15u, 4022u, 933202u },   // Alpha Squad Commander Carvelle
            new[] { 199202u, 16u, 4023u, 13933202u },   // Alpha Squad Commander Carvelle
            new[] { 199202u, 17u, 24019u, 4286886614u },   // Alpha Squad Commander Carvelle
            new[] { 199203u, 3u, 4021u, 22120u },   // Colonel 'Snake' Washington
            new[] { 199203u, 13u, 27120u, 1u },   // Colonel 'Snake' Washington
            new[] { 199203u, 14u, 3672u, 14404004u },   // Colonel 'Snake' Washington
            new[] { 199203u, 15u, 4022u, 933202u },   // Colonel 'Snake' Washington
            new[] { 199203u, 16u, 4023u, 13933202u },   // Colonel 'Snake' Washington
            new[] { 199203u, 17u, 24019u, 4286886614u },   // Colonel 'Snake' Washington
            new[] { 199204u, 3u, 4021u, 22120u },   // Amee Corman
            new[] { 199204u, 13u, 27120u, 1u },   // Amee Corman
            new[] { 199204u, 14u, 3672u, 14404004u },   // Amee Corman
            new[] { 199204u, 15u, 4022u, 933202u },   // Amee Corman
            new[] { 199204u, 16u, 4023u, 13933202u },   // Amee Corman
            new[] { 199204u, 17u, 24019u, 4286886614u },   // Amee Corman
            new[] { 199205u, 3u, 4021u, 22120u },   // General Thaddeus T. Bailey
            new[] { 199205u, 13u, 27120u, 1u },   // General Thaddeus T. Bailey
            new[] { 199205u, 14u, 3672u, 14404004u },   // General Thaddeus T. Bailey
            new[] { 199205u, 15u, 4022u, 933202u },   // General Thaddeus T. Bailey
            new[] { 199205u, 16u, 4023u, 13933202u },   // General Thaddeus T. Bailey
            new[] { 199205u, 17u, 24019u, 4286886614u },   // General Thaddeus T. Bailey
            new[] { 199206u, 3u, 4021u, 22120u },   // Colonel Bosley
            new[] { 199206u, 13u, 27120u, 1u },   // Colonel Bosley
            new[] { 199206u, 14u, 3672u, 14404004u },   // Colonel Bosley
            new[] { 199206u, 15u, 4022u, 933202u },   // Colonel Bosley
            new[] { 199206u, 16u, 4023u, 13933202u },   // Colonel Bosley
            new[] { 199206u, 17u, 24019u, 4286886614u },   // Colonel Bosley
            new[] { 199300u, 3u, 4021u, 22120u },   // Lieutenant Morrison
            new[] { 199300u, 13u, 27120u, 1u },   // Lieutenant Morrison
            new[] { 199300u, 14u, 3672u, 14404004u },   // Lieutenant Morrison
            new[] { 199300u, 15u, 4022u, 933202u },   // Lieutenant Morrison
            new[] { 199300u, 16u, 4023u, 13933202u },   // Lieutenant Morrison
            new[] { 199300u, 17u, 24019u, 4286886614u },   // Lieutenant Morrison
            new[] { 199301u, 3u, 4021u, 22120u },   // Retread Jeska
            new[] { 199301u, 13u, 27120u, 1u },   // Retread Jeska
            new[] { 199301u, 14u, 3672u, 14404004u },   // Retread Jeska
            new[] { 199301u, 15u, 4022u, 933202u },   // Retread Jeska
            new[] { 199301u, 16u, 4023u, 13933202u },   // Retread Jeska
            new[] { 199301u, 17u, 24019u, 4286886614u },   // Retread Jeska
            new[] { 199302u, 3u, 4021u, 22120u },   // Retread Lou
            new[] { 199302u, 13u, 27120u, 1u },   // Retread Lou
            new[] { 199302u, 14u, 3672u, 14404004u },   // Retread Lou
            new[] { 199302u, 15u, 4022u, 933202u },   // Retread Lou
            new[] { 199302u, 16u, 4023u, 13933202u },   // Retread Lou
            new[] { 199302u, 17u, 24019u, 4286886614u },   // Retread Lou
            new[] { 199303u, 3u, 4021u, 22120u },   // Retread Duvall
            new[] { 199303u, 13u, 27120u, 1u },   // Retread Duvall
            new[] { 199303u, 14u, 3672u, 14404004u },   // Retread Duvall
            new[] { 199303u, 15u, 4022u, 933202u },   // Retread Duvall
            new[] { 199303u, 16u, 4023u, 13933202u },   // Retread Duvall
            new[] { 199303u, 17u, 24019u, 4286886614u },   // Retread Duvall
            new[] { 199400u, 3u, 4021u, 22120u },   // Sgt. Jeansonne
            new[] { 199400u, 13u, 27120u, 1u },   // Sgt. Jeansonne
            new[] { 199400u, 14u, 3672u, 14404004u },   // Sgt. Jeansonne
            new[] { 199400u, 15u, 4022u, 933202u },   // Sgt. Jeansonne
            new[] { 199400u, 16u, 4023u, 13933202u },   // Sgt. Jeansonne
            new[] { 199400u, 17u, 24019u, 4286886614u },   // Sgt. Jeansonne
            new[] { 199401u, 3u, 4021u, 22120u },   // Chakel
            new[] { 199401u, 13u, 27120u, 1u },   // Chakel
            new[] { 199401u, 14u, 3672u, 14404004u },   // Chakel
            new[] { 199401u, 15u, 4022u, 933202u },   // Chakel
            new[] { 199401u, 16u, 4023u, 13933202u },   // Chakel
            new[] { 199401u, 17u, 24019u, 4286886614u },   // Chakel
            new[] { 199402u, 3u, 4021u, 22120u },   // Corporal Cooper
            new[] { 199402u, 13u, 27120u, 1u },   // Corporal Cooper
            new[] { 199402u, 14u, 3672u, 14404004u },   // Corporal Cooper
            new[] { 199402u, 15u, 4022u, 933202u },   // Corporal Cooper
            new[] { 199402u, 16u, 4023u, 13933202u },   // Corporal Cooper
            new[] { 199402u, 17u, 24019u, 4286886614u },   // Corporal Cooper
            new[] { 199403u, 3u, 4021u, 22120u },   // Lt. Foushee
            new[] { 199403u, 13u, 27120u, 1u },   // Lt. Foushee
            new[] { 199403u, 14u, 3672u, 14404004u },   // Lt. Foushee
            new[] { 199403u, 15u, 4022u, 933202u },   // Lt. Foushee
            new[] { 199403u, 16u, 4023u, 13933202u },   // Lt. Foushee
            new[] { 199403u, 17u, 24019u, 4286886614u },   // Lt. Foushee
            new[] { 199404u, 3u, 4021u, 22120u },   // Corporal Hairston
            new[] { 199404u, 13u, 27120u, 1u },   // Corporal Hairston
            new[] { 199404u, 14u, 3672u, 14404004u },   // Corporal Hairston
            new[] { 199404u, 15u, 4022u, 933202u },   // Corporal Hairston
            new[] { 199404u, 16u, 4023u, 13933202u },   // Corporal Hairston
            new[] { 199404u, 17u, 24019u, 4286886614u },   // Corporal Hairston
            new[] { 199405u, 3u, 4021u, 22120u },   // Receptive Liaison Ridout
            new[] { 199405u, 13u, 27120u, 1u },   // Receptive Liaison Ridout
            new[] { 199405u, 14u, 3672u, 14404004u },   // Receptive Liaison Ridout
            new[] { 199405u, 15u, 4022u, 933202u },   // Receptive Liaison Ridout
            new[] { 199405u, 16u, 4023u, 13933202u },   // Receptive Liaison Ridout
            new[] { 199405u, 17u, 24019u, 4286886614u },   // Receptive Liaison Ridout
            new[] { 199500u, 3u, 4021u, 22120u },   // Colonel Whitaker
            new[] { 199500u, 13u, 27120u, 1u },   // Colonel Whitaker
            new[] { 199500u, 14u, 3672u, 14404004u },   // Colonel Whitaker
            new[] { 199500u, 15u, 4022u, 933202u },   // Colonel Whitaker
            new[] { 199500u, 16u, 4023u, 13933202u },   // Colonel Whitaker
            new[] { 199500u, 17u, 24019u, 4286886614u },   // Colonel Whitaker
            new[] { 199501u, 3u, 4021u, 22120u },   // Xenori
            new[] { 199501u, 13u, 27120u, 1u },   // Xenori
            new[] { 199501u, 14u, 3672u, 14404004u },   // Xenori
            new[] { 199501u, 15u, 4022u, 933202u },   // Xenori
            new[] { 199501u, 16u, 4023u, 13933202u },   // Xenori
            new[] { 199501u, 17u, 24019u, 4286886614u },   // Xenori
            new[] { 199502u, 3u, 4021u, 22120u },   // Captain Reyko
            new[] { 199502u, 13u, 27120u, 1u },   // Captain Reyko
            new[] { 199502u, 14u, 3672u, 14404004u },   // Captain Reyko
            new[] { 199502u, 15u, 4022u, 933202u },   // Captain Reyko
            new[] { 199502u, 16u, 4023u, 13933202u },   // Captain Reyko
            new[] { 199502u, 17u, 24019u, 4286886614u },   // Captain Reyko
            new[] { 199503u, 3u, 4021u, 22120u },   // Engineer Tralos
            new[] { 199503u, 13u, 27120u, 1u },   // Engineer Tralos
            new[] { 199503u, 14u, 3672u, 14404004u },   // Engineer Tralos
            new[] { 199503u, 15u, 4022u, 933202u },   // Engineer Tralos
            new[] { 199503u, 16u, 4023u, 13933202u },   // Engineer Tralos
            new[] { 199503u, 17u, 24019u, 4286886614u },   // Engineer Tralos
            new[] { 199504u, 3u, 4021u, 22120u },   // Receptive Liaison Sage
            new[] { 199504u, 13u, 27120u, 1u },   // Receptive Liaison Sage
            new[] { 199504u, 14u, 3672u, 14404004u },   // Receptive Liaison Sage
            new[] { 199504u, 15u, 4022u, 933202u },   // Receptive Liaison Sage
            new[] { 199504u, 16u, 4023u, 13933202u },   // Receptive Liaison Sage
            new[] { 199504u, 17u, 24019u, 4286886614u },   // Receptive Liaison Sage
            new[] { 199505u, 3u, 4021u, 22120u },   // Receptive Liaison Repp
            new[] { 199505u, 13u, 27120u, 1u },   // Receptive Liaison Repp
            new[] { 199505u, 14u, 3672u, 14404004u },   // Receptive Liaison Repp
            new[] { 199505u, 15u, 4022u, 933202u },   // Receptive Liaison Repp
            new[] { 199505u, 16u, 4023u, 13933202u },   // Receptive Liaison Repp
            new[] { 199505u, 17u, 24019u, 4286886614u },   // Receptive Liaison Repp
            new[] { 199600u, 3u, 4021u, 22120u },   // Receptive Liaison Maddox
            new[] { 199600u, 13u, 27120u, 1u },   // Receptive Liaison Maddox
            new[] { 199600u, 14u, 3672u, 14404004u },   // Receptive Liaison Maddox
            new[] { 199600u, 15u, 4022u, 933202u },   // Receptive Liaison Maddox
            new[] { 199600u, 16u, 4023u, 13933202u },   // Receptive Liaison Maddox
            new[] { 199600u, 17u, 24019u, 4286886614u },   // Receptive Liaison Maddox
            new[] { 199601u, 3u, 4021u, 22120u },   // Supply Sergeant Otto
            new[] { 199601u, 13u, 27120u, 1u },   // Supply Sergeant Otto
            new[] { 199601u, 14u, 3672u, 14404004u },   // Supply Sergeant Otto
            new[] { 199601u, 15u, 4022u, 933202u },   // Supply Sergeant Otto
            new[] { 199601u, 16u, 4023u, 13933202u },   // Supply Sergeant Otto
            new[] { 199601u, 17u, 24019u, 4286886614u },   // Supply Sergeant Otto
            new[] { 199602u, 3u, 4021u, 22120u },   // Private Parsons
            new[] { 199602u, 13u, 27120u, 1u },   // Private Parsons
            new[] { 199602u, 14u, 3672u, 14404004u },   // Private Parsons
            new[] { 199602u, 15u, 4022u, 933202u },   // Private Parsons
            new[] { 199602u, 16u, 4023u, 13933202u },   // Private Parsons
            new[] { 199602u, 17u, 24019u, 4286886614u },   // Private Parsons
            new[] { 199603u, 3u, 4021u, 22120u },   // Lt. Gerry
            new[] { 199603u, 13u, 27120u, 1u },   // Lt. Gerry
            new[] { 199603u, 14u, 3672u, 14404004u },   // Lt. Gerry
            new[] { 199603u, 15u, 4022u, 933202u },   // Lt. Gerry
            new[] { 199603u, 16u, 4023u, 13933202u },   // Lt. Gerry
            new[] { 199603u, 17u, 24019u, 4286886614u },   // Lt. Gerry
            new[] { 199800u, 3u, 4021u, 22120u },   // Professor Long
            new[] { 199800u, 13u, 27120u, 1u },   // Professor Long
            new[] { 199800u, 14u, 3672u, 14404004u },   // Professor Long
            new[] { 199800u, 15u, 4022u, 933202u },   // Professor Long
            new[] { 199800u, 16u, 4023u, 13933202u },   // Professor Long
            new[] { 199800u, 17u, 24019u, 4286886614u },   // Professor Long
            new[] { 199801u, 2u, 3941u, 12429948u },   // Dr. Robertson [doctor]
            new[] { 199801u, 14u, 3672u, 1440400u },   // Dr. Robertson [doctor]
            new[] { 199801u, 15u, 3942u, 12429948u },   // Dr. Robertson [doctor]
            new[] { 199801u, 16u, 3943u, 12429948u },   // Dr. Robertson [doctor]
            new[] { 199801u, 17u, 24019u, 4286886614u },   // Dr. Robertson [doctor]
            new[] { 199802u, 3u, 4021u, 22120u },   // Colonel Li Hua
            new[] { 199802u, 13u, 27120u, 1u },   // Colonel Li Hua
            new[] { 199802u, 14u, 3672u, 14404004u },   // Colonel Li Hua
            new[] { 199802u, 15u, 4022u, 933202u },   // Colonel Li Hua
            new[] { 199802u, 16u, 4023u, 13933202u },   // Colonel Li Hua
            new[] { 199802u, 17u, 24019u, 4286886614u },   // Colonel Li Hua
            new[] { 199803u, 3u, 4021u, 22120u },   // Ranger Milpas (client name 9519)
            new[] { 199803u, 13u, 27120u, 1u },   // Ranger Milpas (client name 9519)
            new[] { 199803u, 14u, 3672u, 14404004u },   // Ranger Milpas (client name 9519)
            new[] { 199803u, 15u, 4022u, 933202u },   // Ranger Milpas (client name 9519)
            new[] { 199803u, 16u, 4023u, 13933202u },   // Ranger Milpas (client name 9519)
            new[] { 199803u, 17u, 24019u, 4286886614u },   // Ranger Milpas (client name 9519)
        };

        public static void InsertData(MigrationBuilder migrationBuilder)
        {
            foreach (var row in Rows)
                migrationBuilder.InsertData(
                    table: "creature_appearance",
                    columns: new[] { "id", "slot_id", "Class_id", "color" },
                    values: new object[] { row[0], row[1], row[2], row[3] });
        }

        public static void DeleteData(MigrationBuilder migrationBuilder)
        {
            foreach (var row in Rows)
                migrationBuilder.DeleteData(
                    table: "creature_appearance",
                    keyColumns: new[] { "id", "slot_id" },
                    keyValues: new object[] { row[0], row[1] });
        }
    }
}
