using RHToolkit.Models.Localization;
using RHToolkit.Properties;
using System.ComponentModel;

namespace RHToolkit.Models
{
    public class EnumService
    {
        public static List<NameID> GetEnumItems<T>(bool addAll = false) where T : Enum
        {
            List<NameID> enumItems = [];

            if (addAll)
            {
                enumItems.Add(new NameID { ID = 0, Name = Resources.All });
            }

            foreach (T enumValue in Enum.GetValues(typeof(T)))
            {
                string description = GetEnumDescription(enumValue);
                enumItems.Add(new NameID { ID = Convert.ToInt32(enumValue), Name = description });
            }

            return enumItems;
        }

        public static List<NameID> GetSocketColorItems()
        {
            List<NameID> socketColorItems = [];

            foreach (SocketColor color in Enum.GetValues(typeof(SocketColor)))
            {
                int id = (int)color;
                string description = GetEnumDescription(color);
                string imagePath = GetSocketColorImagePath(color);
                socketColorItems.Add(new NameID { ID = id, Name = description, ImagePath = imagePath });
            }

            return socketColorItems;
        }

        private static string GetSocketColorImagePath(SocketColor color)
        {
            return color switch
            {
                SocketColor.None => "/Assets/images/socket_a.png",
                SocketColor.Red => "/Assets/images/socket_r.png",
                SocketColor.Blue => "/Assets/images/socket_b.png",
                SocketColor.Yellow => "/Assets/images/socket_y.png",
                SocketColor.Green => "/Assets/images/socket_g.png",
                SocketColor.Colorless => "/Assets/images/socket_all.png",
                SocketColor.Gray => "/Assets/images/socket_a.png",
                _ => "/Assets/images/socket_a.png",
            };
        }


        public static string GetEnumDescription(Enum value)
        {
            FieldInfo? field = value.GetType().GetField(value.ToString());
            DescriptionAttribute? attribute = field?.GetCustomAttribute<DescriptionAttribute>();
            return attribute == null ? value.ToString() : attribute.Description;
        }

        public static Enum GetJobEnum(CharClass charClass, int jobValue)
        {
            return charClass switch
            {
                CharClass.Frantz or CharClass.Roselle or CharClass.Leila => (FrantzJob)jobValue,
                CharClass.Angela or CharClass.Edgar => (AngelaJob)jobValue,
                CharClass.Tude or CharClass.Meilin => (TudeJob)jobValue,
                CharClass.Natasha or CharClass.Ian => (NatashaJob)jobValue,
                _ => throw new ArgumentException($"Invalid character class: {charClass}"),
            };
        }

        public enum CharClass
        {
            [LocalizedDescription("Frantz")]
            Frantz = 1,
            [LocalizedDescription("Angela")]
            Angela = 2,
            [LocalizedDescription("Tude")]
            Tude = 3,
            [LocalizedDescription("Natasha")]
            Natasha = 4,
            [LocalizedDescription("Roselle")]
            Roselle = 101,
            [LocalizedDescription("Leila")]
            Leila = 102,
            [LocalizedDescription("Edgar")]
            Edgar = 201,
            [LocalizedDescription("Meilin")]
            Meilin = 301,
            [LocalizedDescription("Ian")]
            Ian = 401
        }

        public enum FrantzJob
        {
            [LocalizedDescription("Basic")]
            Basic = 0,
            [LocalizedDescription("Sword")]
            Sword = 1,
            [LocalizedDescription("Axe")]
            Axe = 2,
            [LocalizedDescription("TwinSwords")]
            TwinSwords = 3
        }

        public enum AngelaJob
        {
            [LocalizedDescription("Basic")]
            Basic = 0,
            [LocalizedDescription("MagicSword")]
            MagicSword = 1,
            [LocalizedDescription("Scythe")]
            Scythe = 2,
            [LocalizedDescription("Glaive")]
            Glaive = 3
        }

        public enum TudeJob
        {
            [LocalizedDescription("Basic")]
            Basic = 0,
            [LocalizedDescription("Gauntlet")]
            Gauntlet = 1,
            [LocalizedDescription("Claw")]
            Claw = 2,
            [LocalizedDescription("DemonHands")]
            DemonHands = 3
        }

        public enum NatashaJob
        {
            [LocalizedDescription("Basic")]
            Basic = 0,
            [LocalizedDescription("Revolver")]
            Revolver = 1,
            [LocalizedDescription("Musket")]
            Musket = 2,
            [LocalizedDescription("WeaponBag")]
            WeaponBag = 3
        }

        public enum Branch
        {
            [LocalizedDescription("Normal")]
            Normal = 1,
            [LocalizedDescription("Magic")]
            Magic = 2,
            [LocalizedDescription("Rare")]
            Rare = 4,
            [LocalizedDescription("Unique")]
            Unique = 5,
            [LocalizedDescription("Epic")]
            Epic = 6
        }

        public enum SocketColor
        {
            [LocalizedDescription("SocketColorNone")]
            None = 0,
            [LocalizedDescription("SocketColorRed")]
            Red = 1,
            [LocalizedDescription("SocketColorBlue")]
            Blue = 2,
            [LocalizedDescription("SocketColorYellow")]
            Yellow = 3,
            [LocalizedDescription("SocketColorGreen")]
            Green = 4,
            [LocalizedDescription("SocketColorColorless")]
            Colorless = 5,
            [LocalizedDescription("SocketColorGray")]
            Gray = 6
        }

        public enum ItemType
        {
            [LocalizedDescription("All")]
            All,
            [LocalizedDescription("Item")]
            Item,
            [LocalizedDescription("Costume")]
            Costume,
            [LocalizedDescription("Armor")]
            Armor,
            [LocalizedDescription("Weapon")]
            Weapon
        }

        public enum SanctionOperationType
        {
            Add = 1,
            Remove = 2
        }

        public enum SanctionType
        {
            [LocalizedDescription("SanctionHacking")]
            Hacking = 1,
            [LocalizedDescription("SanctionAccountSteal")]
            AccountSteal = 2,
            [LocalizedDescription("SanctionAbusing")]
            Abusing = 3,
            [LocalizedDescription("SanctionImpersonation")]
            ImpersonationScam = 4,
            [LocalizedDescription("SanctionBugExploiting")]
            BugExploiting = 5,
            [LocalizedDescription("SanctionScam")]
            Scam = 6,
            [LocalizedDescription("SanctionAbusingLanguage")]
            AbusiveLanguage = 7,
            [LocalizedDescription("SanctionAdvertising")]
            CommercialAdvertising = 8,
            [LocalizedDescription("SanctionFalseReport")]
            FalseReport = 9
        }

        public enum SanctionCount
        {
            [LocalizedDescription("SanctionFirst")]
            First = 1,
            [LocalizedDescription("SanctionSecond")]
            Second = 2,
            [LocalizedDescription("SanctionThird")]
            Third = 3,
            [LocalizedDescription("SanctionFourth")]
            Fourth = 4,
            [LocalizedDescription("SanctionFifth")]
            Fifth = 5
        }

        public enum SanctionPeriod
        {
            [LocalizedDescription("Sanction3Days")]
            ThreeDays = 1,
            [LocalizedDescription("Sanction7Days")]
            SevenDays = 2,
            [LocalizedDescription("Sanction10Days")]
            TenDays = 3,
            [LocalizedDescription("Sanction15Days")]
            FifteenDays = 4,
            [LocalizedDescription("SanctionPermanent")]
            Permanent = 5
        }

        public enum EquipCategory
        {
            [LocalizedDescription("EquipCategory0")]
            Weapon = 0,

            [LocalizedDescription("EquipCategory1")]
            Chest = 1,

            [LocalizedDescription("EquipCategory2")]
            Head = 2,

            [LocalizedDescription("EquipCategory3")]
            Legs = 3,

            [LocalizedDescription("EquipCategory4")]
            Feet = 4,

            [LocalizedDescription("EquipCategory5")]
            Waist = 5,

            [LocalizedDescription("EquipCategory6")]
            Necklace = 6,

            [LocalizedDescription("EquipCategory7")]
            Earrings = 7,

            [LocalizedDescription("EquipCategory8")]
            Ring = 8,

            [LocalizedDescription("EquipCategory9")]
            Hands = 9,

            [LocalizedDescription("EquipCategory10")]
            Hair = 10,

            [LocalizedDescription("EquipCategory11")]
            Face = 11,

            [LocalizedDescription("EquipCategory12")]
            Neck = 12,

            [LocalizedDescription("EquipCategory13")]
            Outerwear = 13,

            [LocalizedDescription("EquipCategory14")]
            Top = 14,

            [LocalizedDescription("EquipCategory15")]
            Bottom = 15,

            [LocalizedDescription("EquipCategory16")]
            Gloves = 16,

            [LocalizedDescription("EquipCategory17")]
            Shoes = 17,

            [LocalizedDescription("EquipCategory18")]
            Accessory1 = 18,

            [LocalizedDescription("EquipCategory19")]
            Accessory2 = 19,
        }

        public enum InventoryType
        {
            [LocalizedDescription("Equipment")]
            Equipment = 1,

            [LocalizedDescription("Consume")]
            Consume = 2,

            [LocalizedDescription("Other")]
            Other = 3,

            [LocalizedDescription("Quest")]
            Quest = 4,

            [LocalizedDescription("Costume")]
            Costume = 5,

            [LocalizedDescription("TriggerItem")]
            Trigger = 6,

            [LocalizedDescription("QuickSlot")]
            QuickSlot = 11,

            [LocalizedDescription("Storage")]
            Storage = 21,
        }

    }
}
