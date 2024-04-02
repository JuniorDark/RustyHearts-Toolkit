using System.ComponentModel;
using System.Reflection;

namespace RHGMTool.Models
{
    public class EnumService
    {
        public static List<NameID> GetEnumItems<T>() where T : Enum
        {
            List<NameID> enumItems = [];

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
            [Description("Frantz")]
            Frantz = 1,
            [Description("Angela")]
            Angela = 2,
            [Description("Tude")]
            Tude = 3,
            [Description("Natasha")]
            Natasha = 4,
            [Description("Roselle")]
            Roselle = 101,
            [Description("Leila")]
            Leila = 102,
            [Description("Edgar")]
            Edgar = 201,
            [Description("Meilin")]
            Meilin = 301,
            [Description("Ian")]
            Ian = 401
        }

        public enum FrantzJob
        {
            [Description("Basic")]
            Basic = 0,
            [Description("Sword")]
            Sword = 1,
            [Description("Axe")]
            Axe = 2,
            [Description("Twin Swords")]
            TwinSwords = 3
        }

        public enum AngelaJob
        {
            [Description("Basic")]
            Basic = 0,
            [Description("Magic Sword")]
            MagicSword = 1,
            [Description("Scythe")]
            Scythe = 2,
            [Description("Glaive")]
            Glaive = 3
        }

        public enum TudeJob
        {
            [Description("Basic")]
            Basic = 0,
            [Description("Gauntlet")]
            Gauntlet = 1,
            [Description("Claw")]
            Claw = 2,
            [Description("Demon Hands")]
            DemonHands = 3
        }

        public enum NatashaJob
        {
            [Description("Basic")]
            Basic = 0,
            [Description("Revolver")]
            Revolver = 1,
            [Description("Musket")]
            Musket = 2,
            [Description("Weapon Bag")]
            WeaponBag = 3
        }

        public enum Branch
        {
            [Description("Normal")]
            Normal = 1,
            [Description("Magic")]
            Magic = 2,
            [Description("Rare")]
            Rare = 3,
            [Description("Unique")]
            Unique = 4,
            [Description("Epic")]
            Epic = 5
        }

        public static IEnumerable<int> MapBranchIndexToValues(int branchIndex)
        {
            return branchIndex switch
            {
                0 => [0], // All branches
                1 => [1],
                2 => [2],
                3 => [4],
                4 => [5],
                5 => [6],
                6 => [5],
                _ => [0],
            };
        }

        public enum SocketColor
        {
            [Description("None")]
            None = 0,
            [Description("Red")]
            Red = 1,
            [Description("Blue")]
            Blue = 2,
            [Description("Yellow")]
            Yellow = 3,
            [Description("Green")]
            Green = 4,
            [Description("Colorless")]
            Colorless = 5,
            [Description("Gray")]
            Gray = 6
        }

        public enum ItemType
        {
            [Description("All")]
            All,
            [Description("Item")]
            Item,
            [Description("Costume")]
            Costume,
            [Description("Armor")]
            Armor,
            [Description("Weapon")]
            Weapon
        }

        public enum SanctionOperationType
        {
            Add,
            Remove
        }

        public enum SanctionType
        {
            [Description("Hacking")]
            Hacking = 1,
            [Description("Account Steal")]
            AccountSteal = 2,
            [Description("Abusing")]
            Abusing = 3,
            [Description("Impersonation/Scam")]
            ImpersonationScam = 4,
            [Description("Bug Exploiting")]
            BugExploiting = 5,
            [Description("Scam")]
            Scam = 6,
            [Description("Use of Sexual/Abusive Language")]
            AbusiveLanguage = 7,
            [Description("Commercial Advertising")]
            CommercialAdvertising = 8,
            [Description("False Report")]
            FalseReport = 9
        }

        public enum SanctionCount
        {
            [Description("First")]
            First = 1,
            [Description("Second")]
            Second = 2,
            [Description("Third")]
            Third = 3,
            [Description("Fourth")]
            Fourth = 4,
            [Description("Fifth")]
            Fifth = 5
        }

        public enum SanctionPeriod
        {
            [Description("3 Days")]
            ThreeDays = 1,
            [Description("7 Days")]
            SevenDays = 2,
            [Description("10 Days")]
            TenDays = 3,
            [Description("15 Days")]
            FifteenDays = 4,
            [Description("Permanent")]
            Permanent = 5
        }
    }
}
