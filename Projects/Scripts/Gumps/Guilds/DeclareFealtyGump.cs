using Server.Guilds;
using Server.Network;

namespace Server.Gumps
{
  public class DeclareFealtyGump : GuildMobileListGump
  {
    public DeclareFealtyGump(Mobile from, Guild guild) : base(from, guild, true, guild.Members)
    {
    }

    protected override void Design()
    {
      AddHtmlLocalized(20, 10, 400, 35, 1011097); // Declare your fealty

      AddButton(20, 400, 4005, 4007, 1);
      AddHtmlLocalized(55, 400, 250, 35, 1011098); // I have selected my new lord.

      AddButton(300, 400, 4005, 4007, 0);
      AddHtmlLocalized(335, 400, 100, 35, 1011012); // CANCEL
    }

    public override void OnResponse(NetState sender, RelayInfo info)
    {
      if (GuildGump.BadMember(m_Mobile, m_Guild))
        return;

      if (info.ButtonID == 1)
      {
        int[] switches = info.Switches;

        if (switches.Length > 0)
        {
          int index = switches[0];

          if (index >= 0 && index < m_List.Count)
          {
            Mobile m = m_List[index];

            if (m?.Deleted == false)
              state.Mobile.GuildFealty = m;
          }
        }
      }

      GuildGump.EnsureClosed(m_Mobile);
      m_Mobile.SendGump(new GuildGump(m_Mobile, m_Guild));
    }
  }
}