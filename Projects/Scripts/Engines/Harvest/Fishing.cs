using System;
using System.Collections.Generic;
using System.Linq;
using Server.Engines.Quests;
using Server.Engines.Quests.Collector;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Spells;

namespace Server.Engines.Harvest
{
  public class Fishing : HarvestSystem
  {
    private static Fishing m_System;

    private static MutateEntry[] m_MutateTable =
    {
      new MutateEntry(80.0, 80.0, 4080.0, true, typeof(SpecialFishingNet)),
      new MutateEntry(80.0, 80.0, 4080.0, true, typeof(BigFish)),
      new MutateEntry(90.0, 80.0, 4080.0, true, typeof(TreasureMap)),
      new MutateEntry(100.0, 80.0, 4080.0, true, typeof(MessageInABottle)),
      new MutateEntry(0.0, 125.0, -2375.0, false, typeof(PrizedFish), typeof(WondrousFish), typeof(TrulyRareFish),
        typeof(PeculiarFish)),
      new MutateEntry(0.0, 105.0, -420.0, false, typeof(Boots), typeof(Shoes), typeof(Sandals), typeof(ThighBoots)),
      new MutateEntry(0.0, 200.0, -200.0, false, new Type[1] { null })
    };

    private static int[] m_WaterTiles =
    {
      0x00A8, 0x00AB,
      0x0136, 0x0137,
      0x5797, 0x579C,
      0x746E, 0x7485,
      0x7490, 0x74AB,
      0x74B5, 0x75D5
    };

    private Fishing()
    {
      HarvestDefinition fish = new HarvestDefinition
      {
        BankWidth = 8,
        BankHeight = 8,
        MinTotal = 5,
        MaxTotal = 15,
        MinRespawn = TimeSpan.FromMinutes(10.0),
        MaxRespawn = TimeSpan.FromMinutes(20.0),
        Skill = SkillName.Fishing,
        Tiles = m_WaterTiles,
        RangedTiles = true,
        MaxRange = 4,
        ConsumedPerHarvest = 1,
        ConsumedPerFeluccaHarvest = 1,
        EffectActions = new[] { 12 },
        EffectSounds = new int[0],
        EffectCounts = new[] { 1 },
        EffectDelay = TimeSpan.Zero,
        EffectSoundDelay = TimeSpan.FromSeconds(8.0),
        NoResourcesMessage = 503172, // The fish don't seem to be biting here.
        FailMessage = 503171, // You fish a while, but fail to catch anything.
        TimedOutOfRangeMessage = 500976, // You need to be closer to the water to fish!
        OutOfRangeMessage = 500976, // You need to be closer to the water to fish!
        PackFullMessage = 503176, // You do not have room in your backpack for a fish.
        ToolBrokeMessage = 503174 // You broke your fishing pole.
      };

      HarvestResource[] res = {
        new HarvestResource(00.0, 00.0, 100.0, 1043297, typeof(Fish))
      };

      HarvestVein[] veins = {
        new HarvestVein(100.0, 0.0, res[0], null)
      };

      fish.Resources = res;
      fish.Veins = veins;

      if (Core.ML)
        fish.BonusResources = new[]
        {
          new BonusHarvestResource(0, 99.4, null, null), //set to same chance as mining ml gems
          new BonusHarvestResource(80.0, .6, 1072597, typeof(WhitePearl))
        };

      Definition = fish;
      Definitions.Add(fish);
    }

    public static Fishing System => m_System ??= new Fishing();

    public HarvestDefinition Definition{ get; }

    public override void OnConcurrentHarvest(Mobile from, Item tool, HarvestDefinition def, object toHarvest)
    {
      from.SendLocalizedMessage(500972); // You are already fishing.
    }

    public override bool SpecialHarvest(Mobile from, Item tool, HarvestDefinition def, Map map, Point3D loc)
    {
      if (from is PlayerMobile player)
      {
        QuestSystem qs = player.Quest;

        if (qs is CollectorQuest)
        {
          QuestObjective obj = qs.FindObjective<FishPearlsObjective>();

          if (obj?.Completed == false)
          {
            if (Utility.RandomDouble() < 0.5)
            {
              player.SendLocalizedMessage(1055086, "",
                0x59); // You pull a shellfish out of the water, and find a rainbow pearl inside of it.

              obj.CurProgress++;
            }
            else
            {
              player.SendLocalizedMessage(1055087, "",
                0x2C); // You pull a shellfish out of the water, but it doesn't have a rainbow pearl.
            }

            return true;
          }
        }
      }

      return false;
    }

    public override Type MutateType(Type type, Mobile from, Item tool, HarvestDefinition def, Map map, Point3D loc,
      HarvestResource resource)
    {
      bool deepWater = SpecialFishingNet.FullValidation(map, loc.X, loc.Y);

      double skillBase = from.Skills.Fishing.Base;
      double skillValue = from.Skills.Fishing.Value;

      for (int i = 0; i < m_MutateTable.Length; ++i)
      {
        MutateEntry entry = m_MutateTable[i];

        if (!deepWater && entry.m_DeepWater)
          continue;

        if (skillBase >= entry.m_ReqSkill)
        {
          double chance = (skillValue - entry.m_MinSkill) / (entry.m_MaxSkill - entry.m_MinSkill);

          if (chance > Utility.RandomDouble())
            return entry.m_Types[Utility.Random(entry.m_Types.Length)];
        }
      }

      return type;
    }

    private static Map SafeMap(Map map)
    {
      return map == null || map == Map.Internal ? Map.Trammel : map;
    }

    public override bool CheckResources(Mobile from, Item tool, HarvestDefinition def, Map map, Point3D loc, bool timed)
    {
      return from?.Backpack?.FindItemsByType<SOS>().Any(sos =>
               (from.Map == Map.Felucca || from.Map == Map.Trammel) && from.InRange(sos.TargetLocation, 60)) ??
             base.CheckResources(from, tool, def, map, loc, timed);
    }

    public override Item Construct(Type type, Mobile from)
    {
      if (type == typeof(TreasureMap))
      {
        int level;
        if (from is PlayerMobile mobile && mobile.Young && mobile.Map == Map.Trammel &&
            TreasureMap.IsInHavenIsland(from))
          level = 0;
        else
          level = 1;

        return new TreasureMap(level, from.Map == Map.Felucca ? Map.Felucca : Map.Trammel);
      }

      if (type == typeof(MessageInABottle))
        return new MessageInABottle(from.Map == Map.Felucca ? Map.Felucca : Map.Trammel);

      Container pack = from.Backpack;

      if (pack != null)
      {
        List<SOS> messages = pack.FindItemsByType<SOS>();

        for (int i = 0; i < messages.Count; ++i)
        {
          SOS sos = messages[i];

          if ((from.Map == Map.Felucca || from.Map == Map.Trammel) && from.InRange(sos.TargetLocation, 60))
          {
            Item preLoot = null;

            switch (Utility.Random(8))
            {
              case 0: // Body parts
              {
                int[] list =
                {
                  0x1CDD, 0x1CE5, // arm
                  0x1CE0, 0x1CE8, // torso
                  0x1CE1, 0x1CE9, // head
                  0x1CE2, 0x1CEC // leg
                };

                preLoot = new ShipwreckedItem(Utility.RandomList(list));
                break;
              }
              case 1: // Bone parts
              {
                int[] list =
                {
                  0x1AE0, 0x1AE1, 0x1AE2, 0x1AE3, 0x1AE4, // skulls
                  0x1B09, 0x1B0A, 0x1B0B, 0x1B0C, 0x1B0D, 0x1B0E, 0x1B0F, 0x1B10, // bone piles
                  0x1B15, 0x1B16 // pelvis bones
                };

                preLoot = new ShipwreckedItem(Utility.RandomList(list));
                break;
              }
              case 2: // Paintings and portraits
              {
                preLoot = new ShipwreckedItem(Utility.Random(0xE9F, 10));
                break;
              }
              case 3: // Pillows
              {
                preLoot = new ShipwreckedItem(Utility.Random(0x13A4, 11));
                break;
              }
              case 4: // Shells
              {
                preLoot = new ShipwreckedItem(Utility.Random(0xFC4, 9));
                break;
              }
              case 5: //Hats
              {
                if (Utility.RandomBool())
                  preLoot = new SkullCap();
                else
                  preLoot = new TricorneHat();

                break;
              }
              case 6: // Misc
              {
                int[] list =
                {
                  0x1EB5, // unfinished barrel
                  0xA2A, // stool
                  0xC1F, // broken clock
                  0x1047, 0x1048, // globe
                  0x1EB1, 0x1EB2, 0x1EB3, 0x1EB4 // barrel staves
                };

                if (Utility.Random(list.Length + 1) == 0)
                  preLoot = new Candelabra();
                else
                  preLoot = new ShipwreckedItem(Utility.RandomList(list));

                break;
              }
            }

            if (preLoot != null)
            {
              ((IShipwreckedItem)preLoot).IsShipwreckedItem = true;
              return preLoot;
            }

            LockableContainer chest;

            if (Utility.RandomBool())
              chest = new MetalGoldenChest();
            else
              chest = new WoodenChest();

            if (sos.IsAncient)
              chest.Hue = 0x481;

            TreasureMapChest.Fill(chest, Math.Max(1, Math.Min(4, sos.Level)));

            chest.DropItem(sos.IsAncient ? new FabledFishingNet() : new SpecialFishingNet());

            chest.Movable = true;
            chest.Locked = false;
            chest.TrapType = TrapType.None;
            chest.TrapPower = 0;
            chest.TrapLevel = 0;

            sos.Delete();

            return chest;
          }
        }
      }

      return base.Construct(type, from);
    }

    public override bool Give(Mobile m, Item item, bool placeAtFeet)
    {
      if (item is TreasureMap || item is MessageInABottle || item is SpecialFishingNet)
      {
        BaseCreature serp;

        if (0.25 > Utility.RandomDouble())
          serp = new DeepSeaSerpent();
        else
          serp = new SeaSerpent();

        int x = m.X, y = m.Y;

        Map map = m.Map;

        for (int i = 0; map != null && i < 20; ++i)
        {
          int tx = m.X - 10 + Utility.Random(21);
          int ty = m.Y - 10 + Utility.Random(21);

          LandTile t = map.Tiles.GetLandTile(tx, ty);

          if (t.Z == -5 && (t.ID >= 0xA8 && t.ID <= 0xAB || t.ID >= 0x136 && t.ID <= 0x137) &&
              !SpellHelper.CheckMulti(new Point3D(tx, ty, -5), map))
          {
            x = tx;
            y = ty;
            break;
          }
        }

        serp.MoveToWorld(new Point3D(x, y, -5), map);

        serp.Home = serp.Location;
        serp.RangeHome = 10;

        serp.PackItem(item);

        m.SendLocalizedMessage(503170); // Uh oh! That doesn't look like a fish!

        return true; // we don't want to give the item to the player, it's on the serpent
      }

      return base.Give(m, item, placeAtFeet || item is BigFish || item is WoodenChest || item is MetalGoldenChest);
    }

    public override void SendSuccessTo(Mobile from, Item item, HarvestResource resource)
    {
      if (item is BigFish fish)
      {
        from.SendLocalizedMessage(1042635); // Your fishing pole bends as you pull a big fish from the depths!
        fish.Fisher = from;
      }
      else if (item is WoodenChest || item is MetalGoldenChest)
      {
        from.SendLocalizedMessage(503175); // You pull up a heavy chest from the depths of the ocean!
      }
      else
      {
        int number;
        string name;

        if (item is BaseMagicFish)
        {
          number = 1008124;
          name = "a mess of small fish";
        }
        else if (item is Fish)
        {
          number = 1008124;
          name = item.ItemData.Name;
        }
        else if (item is BaseShoes)
        {
          number = 1008124;
          name = item.ItemData.Name;
        }
        else if (item is TreasureMap)
        {
          number = 1008125;
          name = "a sodden piece of parchment";
        }
        else if (item is MessageInABottle)
        {
          number = 1008125;
          name = "a bottle, with a message in it";
        }
        else if (item is SpecialFishingNet)
        {
          number = 1008125;
          name = "a special fishing net"; // TODO: this is just a guess--what should it really be named?
        }
        else
        {
          number = 1043297;

          if ((item.ItemData.Flags & TileFlag.ArticleA) != 0)
            name = "a " + item.ItemData.Name;
          else if ((item.ItemData.Flags & TileFlag.ArticleAn) != 0)
            name = "an " + item.ItemData.Name;
          else
            name = item.ItemData.Name;
        }

        NetState ns = from.NetState;

        if (ns == null)
          return;

        if (number == 1043297 || ns.HighSeas)
          from.SendLocalizedMessage(number, name);
        else
          from.SendLocalizedMessage(number, true, name);
      }
    }

    public override void OnHarvestStarted(Mobile from, Item tool, HarvestDefinition def, object toHarvest)
    {
      base.OnHarvestStarted(from, tool, def, toHarvest);

      if (GetHarvestDetails(from, tool, toHarvest, out _, out Map map, out Point3D loc))
        Timer.DelayCall(TimeSpan.FromSeconds(1.5),
          delegate
          {
            if (Core.ML)
              from.RevealingAction();

            Effects.SendLocationEffect(loc, map, 0x352D, 16, 4);
            Effects.PlaySound(loc, map, 0x364);
          });
    }

    public override void OnHarvestFinished(Mobile from, Item tool, HarvestDefinition def, HarvestVein vein,
      HarvestBank bank, HarvestResource resource, object harvested)
    {
      base.OnHarvestFinished(from, tool, def, vein, bank, resource, harvested);

      if (Core.ML)
        from.RevealingAction();
    }

    public override object GetLock(Mobile from, Item tool, HarvestDefinition def, object toHarvest)
    {
      return this;
    }

    public override bool BeginHarvesting(Mobile from, Item tool)
    {
      if (!base.BeginHarvesting(from, tool))
        return false;

      from.SendLocalizedMessage(500974); // What water do you want to fish in?
      return true;
    }

    public override bool CheckHarvest(Mobile from, Item tool)
    {
      if (!base.CheckHarvest(from, tool))
        return false;

      if (from.Mounted)
      {
        from.SendLocalizedMessage(500971); // You can't fish while riding!
        return false;
      }

      return true;
    }

    public override bool CheckHarvest(Mobile from, Item tool, HarvestDefinition def, object toHarvest)
    {
      if (!base.CheckHarvest(from, tool, def, toHarvest))
        return false;

      if (from.Mounted)
      {
        from.SendLocalizedMessage(500971); // You can't fish while riding!
        return false;
      }

      return true;
    }

    private class MutateEntry
    {
      public bool m_DeepWater;
      public double m_ReqSkill, m_MinSkill, m_MaxSkill;
      public Type[] m_Types;

      public MutateEntry(double reqSkill, double minSkill, double maxSkill, bool deepWater, params Type[] types)
      {
        m_ReqSkill = reqSkill;
        m_MinSkill = minSkill;
        m_MaxSkill = maxSkill;
        m_DeepWater = deepWater;
        m_Types = types;
      }
    }
  }
}
