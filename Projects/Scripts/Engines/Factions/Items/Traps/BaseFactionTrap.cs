using System;
using Server.Items;
using Server.Network;

namespace Server.Factions
{
  public enum AllowedPlacing
  {
    Everywhere,

    AnyFactionTown,
    ControlledFactionTown,
    FactionStronghold
  }

  public abstract class BaseFactionTrap : BaseTrap
  {
    private Timer m_Concealing;

    public BaseFactionTrap(Faction f, Mobile m, int itemID) : base(itemID)
    {
      Visible = false;

      Faction = f;
      TimeOfPlacement = DateTime.UtcNow;
      Placer = m;
    }

    public BaseFactionTrap(Serial serial) : base(serial)
    {
    }

    [CommandProperty(AccessLevel.GameMaster)]
    public Faction Faction{ get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public Mobile Placer{ get; set; }

    [CommandProperty(AccessLevel.GameMaster)]
    public DateTime TimeOfPlacement{ get; set; }

    public virtual int EffectSound => 0;

    public virtual int SilverFromDisarm => 100;

    public virtual int MessageHue => 0;

    public virtual int AttackMessage => 0;
    public virtual int DisarmMessage => 0;

    public virtual AllowedPlacing AllowedPlacing => AllowedPlacing.Everywhere;

    public virtual TimeSpan ConcealPeriod => TimeSpan.FromMinutes(1.0);

    public virtual TimeSpan DecayPeriod
    {
      get
      {
        if (Core.AOS)
          return TimeSpan.FromDays(1.0);

        return TimeSpan.MaxValue; // no decay
      }
    }

    public override void OnTrigger(Mobile from)
    {
      if (!IsEnemy(from))
        return;

      Conceal();

      DoVisibleEffect();
      Effects.PlaySound(Location, Map, EffectSound);
      DoAttackEffect(from);

      int silverToAward = from.Alive ? 20 : 40;

      if (silverToAward > 0 && Placer != null && Faction != null)
      {
        PlayerState victimState = PlayerState.Find(from);

        if (victimState?.CanGiveSilverTo(Placer) == true && victimState.KillPoints > 0)
        {
          int silverGiven = Faction.AwardSilver(Placer, silverToAward);

          if (silverGiven > 0)
          {
            // TODO: Get real message
            if (from.Alive)
              Placer.SendMessage("You have earned {0} silver pieces because {1} fell for your trap.",
                silverGiven, from.Name);
            else
              Placer.SendLocalizedMessage(1042736,
                $"{silverGiven} silver\t{from.Name}"); // You have earned ~1_SILVER_AMOUNT~ pieces for vanquishing ~2_PLAYER_NAME~!
          }

          victimState.OnGivenSilverTo(Placer);
        }
      }

      from.LocalOverheadMessage(MessageType.Regular, MessageHue, AttackMessage);
    }

    public abstract void DoVisibleEffect();
    public abstract void DoAttackEffect(Mobile m);

    public virtual int IsValidLocation() => IsValidLocation(GetWorldLocation(), Map);

    public virtual int IsValidLocation(Point3D p, Map m)
    {
      if (m == null)
        return 502956; // You cannot place a trap on that.

      if (Core.ML)
      {
        IPooledEnumerable eable = m.GetItemsInRange(p, 0);
        foreach (Item item in eable)
          if (item is BaseFactionTrap trap && trap.Faction == Faction)
            return 1075263; // There is already a trap belonging to your faction at this location.;

        eable.Free();
      }

      switch (AllowedPlacing)
      {
        case AllowedPlacing.FactionStronghold:
        {
          StrongholdRegion region = Region.Find(p, m).GetRegion<StrongholdRegion>();

          if (region != null && region.Faction == Faction)
            return 0;

          return 1010355; // This trap can only be placed in your stronghold
        }
        case AllowedPlacing.AnyFactionTown:
        {
          Town town = Town.FromRegion(Region.Find(p, m));

          if (town != null)
            return 0;

          return 1010356; // This trap can only be placed in a faction town
        }
        case AllowedPlacing.ControlledFactionTown:
        {
          Town town = Town.FromRegion(Region.Find(p, m));

          if (town != null && town.Owner == Faction)
            return 0;

          return 1010357; // This trap can only be placed in a town your faction controls
        }
      }

      return 0;
    }

    public override void OnMovement(Mobile m, Point3D oldLocation)
    {
      base.OnMovement(m, oldLocation);

      if (!CheckDecay() && CheckRange(m.Location, oldLocation, 6))
        if (Faction.Find(m) != null &&
            (m.Skills.DetectHidden.Value - 80.0) / 20.0 > Utility.RandomDouble())
          PrivateOverheadLocalizedMessage(m, 1010154, MessageHue, "", ""); // [Faction Trap]
    }

    public void PrivateOverheadLocalizedMessage(Mobile to, int number, int hue, string name, string args)
    {
      NetState ns = to?.NetState;

      Packets.SendMessageLocalized(ns, Serial, ItemID, MessageType.Regular, hue, 3, number, name, args);
    }

    public virtual bool CheckDecay()
    {
      TimeSpan decayPeriod = DecayPeriod;

      if (decayPeriod == TimeSpan.MaxValue)
        return false;

      if (TimeOfPlacement + decayPeriod < DateTime.UtcNow)
      {
        Timer.DelayCall(TimeSpan.Zero, Delete);
        return true;
      }

      return false;
    }

    public virtual void BeginConceal()
    {
      m_Concealing?.Stop();

      m_Concealing = Timer.DelayCall(ConcealPeriod, Conceal);
    }

    public virtual void Conceal()
    {
      m_Concealing?.Stop();

      m_Concealing = null;

      if (!Deleted)
        Visible = false;
    }

    public override void Serialize(GenericWriter writer)
    {
      base.Serialize(writer);

      writer.Write(0); // version

      Faction.WriteReference(writer, Faction);
      writer.Write(Placer);
      writer.Write(TimeOfPlacement);

      if (Visible)
        BeginConceal();
    }

    public override void Deserialize(GenericReader reader)
    {
      base.Deserialize(reader);

      int version = reader.ReadInt();

      Faction = Faction.ReadReference(reader);
      Placer = reader.ReadMobile();
      TimeOfPlacement = reader.ReadDateTime();

      if (Visible)
        BeginConceal();

      CheckDecay();
    }

    public override void OnDelete()
    {
      if (Faction?.Traps.Contains(this) == true)
        Faction.Traps.Remove(this);

      base.OnDelete();
    }

    public virtual bool IsEnemy(Mobile mob)
    {
      if (mob.Hidden && mob.AccessLevel > AccessLevel.Player)
        return false;

      if (!mob.Alive || mob.IsDeadBondedPet)
        return false;

      Faction faction = Faction.Find(mob, true);

      if (faction == null && mob is BaseFactionGuard guard)
        faction = guard.Faction;

      return faction != null && faction != Faction;
    }
  }
}
