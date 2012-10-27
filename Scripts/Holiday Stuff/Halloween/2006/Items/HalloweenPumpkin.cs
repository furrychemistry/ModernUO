﻿using System;
using Server;
using Server.Mobiles;

namespace Server.Items
{
	public class HalloweenPumpkin : Item
	{
		private readonly string[] staff =
		{
			"Ryan", "Mark", "Eos", "Athena", "Xavier", "Krrios", "Zippy"
		};

		[Constructable]
		public HalloweenPumpkin()
			: base()
		{
			Weight = Utility.RandomMinMax( 3, 20 );
			ItemID = ( Utility.RandomDouble() <= .02 ) ? 0x4694 + Utility.Random( 2 ) : Utility.RandomList( 0xc6a, 0xc6b, 0xc6c );
		}

		public override void OnDoubleClick( Mobile from )
		{
			this.ItemID = GetItemID( ItemID );

			base.OnDoubleClick( from );
		}

		private int GetItemID( int itemid )
		{
			switch( ItemID )
			{
				case 0x4694: itemid = 0x4691; break;
				case 0x4691: itemid = 0x4694; break;
				case 0x4698: itemid = 0x4695; break;
				case 0x4695: itemid = 0x4698; break;
			}

			return itemid;
		}

		public override void OnItemLifted( Mobile from, Item item )
		{
			base.OnItemLifted( from, item );

			if( item != null && !item.Deleted && item == this && Name == null )
			{
				if( ItemID == 0x4694 || ItemID == 0x4691 || ItemID == 0x4695 || ItemID == 0x4695 )
				{
					if( Utility.RandomBool() )
					{
						BaseCreature pumpkinhead = new PumpkinHead();

						pumpkinhead.MoveToWorld( Location, from.Map );

						Delete();
					}
					else
					{
						Name = String.Format( "{0}'s Jack-O-antern", staff[ Utility.Random( staff.Length ) ] );
					}
				}
			}
		}

		public HalloweenPumpkin( Serial serial )
			: base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( ( int )0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}
	}
}