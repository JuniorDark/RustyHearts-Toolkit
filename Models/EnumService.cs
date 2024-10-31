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
                enumItems.Add(new NameID { ID = 0, Name = Resources.None });
            }

            foreach (T enumValue in Enum.GetValues(typeof(T)))
            {
                string description = GetEnumDescription(enumValue);
                enumItems.Add(new NameID { ID = Convert.ToInt32(enumValue), Name = description });
            }

            return enumItems;
        }

        public static string GetEnumDescription(Enum value)
        {
            FieldInfo? field = value.GetType().GetField(value.ToString());
            DescriptionAttribute? attribute = field?.GetCustomAttribute<DescriptionAttribute>();
            return attribute == null ? value.ToString() : attribute.Description;
        }

        #region Class/Job

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
            TwinSwords = 3,
            [LocalizedDescription("NotUsed")]
            NotUsed = 4
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
            Glaive = 3,
            [LocalizedDescription("NotUsed")]
            NotUsed = 4
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
            DemonHands = 3,
            [LocalizedDescription("NotUsed")]
            NotUsed = 4
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
            WeaponBag = 3,
            [LocalizedDescription("NotUsed")]
            NotUsed = 4
        }

        public enum GenericJob
        {
            [LocalizedDescription("All")]
            All = 0,
            [LocalizedDescription("Focus1")]
            Focus1 = 1,
            [LocalizedDescription("Focus2")]
            Focus2 = 2,
            [LocalizedDescription("Focus3")]
            Focus3 = 3,
            [LocalizedDescription("NotUsed")]
            NotUsed = 4
        }

        #endregion

        #region Item

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

        #endregion

        #region Socket

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

        #endregion

        #region Sanction

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

        #endregion

        #region CashShop

        public enum CashShopCategory
        {
            [LocalizedDescription("Package")]
            Package,
            [LocalizedDescription("Costume")]
            Costume,
            [LocalizedDescription("Item")]
            Item,
            [LocalizedDescription("Pet")]
            Pet,
            [LocalizedDescription("Bonus")]
            Bonus
        }

        public enum CashShopPaymentType
        {
            [LocalizedDescription("Cash")]
            Cash = 0,
            [LocalizedDescription("Bonus")]
            Bonus = 1
        }

        public enum CashShopPackageCategory
        {
            [LocalizedDescription("Package")]
            Package = 0
        }

        public enum CashShopCostumeCategory
        {
            [LocalizedDescription("Hair")]
            Hair = 0,
            [LocalizedDescription("Accessory")]
            Accessory = 1,
            [LocalizedDescription("Neck")]
            Neck = 2,
            [LocalizedDescription("Outerwear")]
            Outerwear = 3,
            [LocalizedDescription("Tops")]
            Tops = 4,
            [LocalizedDescription("Bottoms")]
            Bottoms = 5,
            [LocalizedDescription("Gloves")]
            Gloves = 6,
            [LocalizedDescription("Shoes")]
            Shoes = 7,
            [LocalizedDescription("Face")]
            Face = 8,
            [LocalizedDescription("CostumePack")]
            CostumePack = 9
        }

        public enum CashShopItemCategory
        {
            [LocalizedDescription("Consume")]
            Consume,
            [LocalizedDescription("Upgrade")]
            Upgrade,
            [LocalizedDescription("Special")]
            Special,
            [LocalizedDescription("Guild")]
            Guild
        }

        public enum CashShopPetCategory
        {
            [LocalizedDescription("Obtain")]
            Consume,
            [LocalizedDescription("Upgrade")]
            Upgrade,
            [LocalizedDescription("Costume")]
            Costume
        }

        public enum CashShopBonusCategory
        {
            [LocalizedDescription("Item")]
            Item,
            [LocalizedDescription("Package")]
            Package,
            [LocalizedDescription("CostumePack")]
            CostumePack,
            [LocalizedDescription("Costume")]
            Costume
        }

        public enum CashShopItemState
        {
            [LocalizedDescription("None")]
            None,
            [LocalizedDescription("ItemStateSale")]
            Sale,
            [LocalizedDescription("ItemStateNew")]
            New,
            [LocalizedDescription("ItemStateEvent")]
            Event,
            [LocalizedDescription("ItemStateBest")]
            Best
        }

        public enum CashShopCategoryFilter
        {
            [LocalizedDescription("All")]
            All = -1,
            [LocalizedDescription("Package")]
            Package = 0,
            [LocalizedDescription("Costume")]
            Costume = 1,
            [LocalizedDescription("Item")]
            Item = 2,
            [LocalizedDescription("Pet")]
            Pet = 3,
            [LocalizedDescription("Bonus")]
            Bonus = 4
        }

        public enum CashShopAllCategory
        {
            [LocalizedDescription("All")]
            All = 0
        }

        #endregion

        #region DataTemplate

        public enum ItemDropGroupType
        {
            None = 0,
            ItemDropGroupListF = 1,
            ItemDropGroupList = 2,
            ChampionItemItemDropGroupList = 3,
            EventWorldItemDropGroupList = 4,
            InstanceItemDropGroupList = 5,
            QuestItemDropGroupList = 6,
            WorldInstanceItemDropGroupList = 7,
            WorldItemDropGroupList = 8,
            WorldItemDropGroupListF = 9,
            RiddleBoxDropGroupList = 10,
            RareCardDropGroupList = 11,
            RareCardRewardItemList = 12,
        }

        public enum NpcShopType
        {
            None = 0,
            NpcShop = 1,
            TradeShop = 2,
            ItemMix = 3,
            CostumeMix = 4,
            ShopItemVisibleFilter = 5,
            ItemPreview = 6,
            ItemBroken = 7,
        }

        public enum QuestType
        {
            None = 0,
            Mission = 1,
            MissionReward = 2,
            PartyMission = 3,
            Quest = 4,
            QuestString = 5,
            QuestComplete = 6,
            QuestGroup = 7,
            QuestGroupString = 8,
            QuestGroupComplete = 9,
            QuestGroupRequest = 10,
            QuestRequest = 11,
            QuestAcquire = 12,
            QuestAcquireString = 13
        }

        public enum EnemyType
        {
            None = 0,
            Enemy = 1,
        }

        public enum WorldType
        {
            None = 0,
            World = 1,
            MapSelectCurtis = 2,
            DungeonInfoList = 3,
        }

        #endregion

        #region Skill

        public enum SkillType
        {
            [LocalizedDescription("None")]
            None = 0,
            [LocalizedDescription("Frantz")]
            SkillFrantz = 1,
            [LocalizedDescription("Angela")]
            SkillAngela = 2,
            [LocalizedDescription("Tude")]
            SkillTude = 3,
            [LocalizedDescription("Natasha")]
            SkillNatasha = 4,
            SkillTreeFrantz = 5,
            SkillTreeAngela = 6,
            SkillTreeTude = 7,
            SkillTreeNatasha = 8,
            SkillUIFrantz = 9,
            SkillUIAngela = 10,
            SkillUITude = 11,
            SkillUINatasha = 12,
        }

        public static string GetSkillJob(SkillType characterSkillType, string? characterType)
        {
            Enum characterFocus = characterSkillType switch
            {
                SkillType.SkillFrantz => (FrantzJob)GetJobFocus(characterType),
                SkillType.SkillAngela => (AngelaJob)GetJobFocus(characterType),
                SkillType.SkillTude => (TudeJob)GetJobFocus(characterType),
                SkillType.SkillNatasha => (NatashaJob)GetJobFocus(characterType),
                _ => throw new ArgumentException($"Invalid SkillType: {characterSkillType}")
            };

            return GetEnumDescription(characterFocus);
        }

        private static int GetJobFocus(string? characterType)
        {
            return characterType switch
            {
                "TYPE_ALL" => 0,
                "TYPE_A" => 1,
                "TYPE_B" => 2,
                "TYPE_C" => 3,
                _ => 0,
            };
        }

        public static SkillType GetCharacterSkillType(SkillType skillType)
        {
            switch (skillType)
            {
                case SkillType.SkillFrantz or SkillType.SkillTreeFrantz or SkillType.SkillUIFrantz:
                    skillType = SkillType.SkillFrantz;
                    break;
                case SkillType.SkillAngela or SkillType.SkillTreeAngela or SkillType.SkillAngela or SkillType.SkillUIAngela:
                    skillType = SkillType.SkillAngela;
                    break;
                case SkillType.SkillTude or SkillType.SkillTreeTude or SkillType.SkillUITude:
                    skillType = SkillType.SkillTude;
                    break;
                case SkillType.SkillNatasha or SkillType.SkillTreeNatasha or SkillType.SkillUINatasha:
                    skillType = SkillType.SkillNatasha;
                    break;
            }

            return skillType;
        }

        public enum BaseSkill
        {
            [LocalizedDescription("BaseSkill")]
            Base = 1,
            [LocalizedDescription("BeforeFocusChange")]
            BeforeChange = 2
        }

        public enum NotUsedSkill
        {
            [LocalizedDescription("BaseSkill")]
            Base = 1,
            [LocalizedDescription("NotUsed")]
            NotUsed1 = 2,
            [LocalizedDescription("NotUsed")]
            NotUsed2 = 3,
            [LocalizedDescription("NotUsed")]
            NotUsed3 = 4,
        }

        public enum FrantzSwordSkill
        {
            [LocalizedDescription("BaseSkill")]
            Base = 1,
            [LocalizedDescription("WeaponSkill")]
            Weapon = 2,
            [LocalizedDescription("CurseSkill")]
            Curse = 3,
            [LocalizedDescription("BlackSorcerySkill")]
            BlackSorcery = 4
        }

        public enum FrantzAxeSkill
        {
            [LocalizedDescription("BaseSkill")]
            Base = 1,
            [LocalizedDescription("WeaponSkill")]
            Weapon = 2,
            [LocalizedDescription("CurseSkill")]
            Curse = 3,
            [LocalizedDescription("SummonSkill")]
            Summon = 4
        }

        public enum FrantzTwinSwordSkill
        {
            [LocalizedDescription("BaseSkill")]
            Base = 1,
            [LocalizedDescription("WeaponSkill")]
            Weapon = 2,
            [LocalizedDescription("CurseSkill")]
            Curse = 3,
            [LocalizedDescription("BlackSorcerySkill")]
            BlackSorcery = 4
        }

        public enum AngelaMagicSwordSkill
        {
            [LocalizedDescription("BaseSkill")]
            Base = 1,
            [LocalizedDescription("LightSkill")]
            Light = 2,
            [LocalizedDescription("FireEarthSkills")]
            FireEarth = 3,
            [LocalizedDescription("WindSkill")]
            Wind = 4
        }

        public enum AngelaScytleSwordSkill
        {
            [LocalizedDescription("BaseSkill")]
            Base = 1,
            [LocalizedDescription("LightSkill")]
            Light = 2,
            [LocalizedDescription("FireSkill")]
            Fire = 3,
            [LocalizedDescription("WindWaterSkills")]
            WindWater = 4
        }

        public enum AngelaGlaiveSkill
        {
            [LocalizedDescription("BaseSkill")]
            Base = 1,
            [LocalizedDescription("LightSkill")]
            Light = 2,
            [LocalizedDescription("FireEarthSkills")]
            FireEarth = 3,
            [LocalizedDescription("WindSkill")]
            Wind = 4
        }

        public enum TudeClawSkill
        {
            [LocalizedDescription("BaseSkill")]
            Base = 1,
            [LocalizedDescription("FightingSkills")]
            FightingSkills = 2,
            [LocalizedDescription("SavageGrabSkills")]
            SavageGrab = 3,
            [LocalizedDescription("KiGongShoutSkills")]
            KiGongShout = 4
        }

        public enum TudeGauntletsSkill
        {
            [LocalizedDescription("BaseSkill")]
            Base = 1,
            [LocalizedDescription("FightingSkills")]
            FightingSkills = 2,
            [LocalizedDescription("SavageSkill")]
            Savage = 3,
            [LocalizedDescription("KiGongShoutSkills")]
            KiGongShout = 4
        }

        public enum TudeDemonHandsSkill
        {
            [LocalizedDescription("BaseSkill")]
            Base = 1,
            [LocalizedDescription("FightingSkills")]
            FightingSkills = 2,
            [LocalizedDescription("SavageSkill")]
            Savage = 3,
            [LocalizedDescription("KiGongShoutSkills")]
            KiGongShout = 4
        }

        public enum NatashaRevolverSkill
        {
            [LocalizedDescription("BaseSkill")]
            Base = 1,
            [LocalizedDescription("FirearmSkill")]
            Firearm = 2,
            [LocalizedDescription("TacticalSkill")]
            Tactical = 3,
            [LocalizedDescription("DeviceSkill")]
            Device = 4
        }

        public enum NatashaMusketSkill
        {
            [LocalizedDescription("BaseSkill")]
            Base = 1,
            [LocalizedDescription("FirearmSkill")]
            Firearm = 2,
            [LocalizedDescription("TacticalSkill")]
            Tactical = 3,
            [LocalizedDescription("HeavyWeaponSkill")]
            HeavyWeapon = 4
        }

        public enum NatashaWeaponBagSkill
        {
            [LocalizedDescription("BaseSkill")]
            Base = 1,
            [LocalizedDescription("HeavyWeaponSkill")]
            HeavyWeapon = 2,
            [LocalizedDescription("TacticalSkill")]
            Tactical = 3,
            [LocalizedDescription("DeviceSkill")]
            Device = 4
        }

        public static string GetSkillClassName(SkillType skillType, int job, int skillClass)
        {
            Type? skillEnumType = skillType switch
            {
                SkillType.SkillUIFrantz => job switch
                {
                    0 => typeof(BaseSkill),
                    1 => typeof(FrantzSwordSkill),
                    2 => typeof(FrantzAxeSkill),
                    3 => typeof(FrantzTwinSwordSkill),
                    4 => typeof(NotUsedSkill),
                    _ => throw new ArgumentException($"Invalid job for Frantz : {job}")
                },
                SkillType.SkillUIAngela => job switch
                {
                    0 => typeof(BaseSkill),
                    1 => typeof(AngelaMagicSwordSkill),
                    2 => typeof(AngelaScytleSwordSkill),
                    3 => typeof(AngelaGlaiveSkill),
                    4 => typeof(NotUsedSkill),
                    _ => throw new ArgumentException($"Invalid job for Angela : {job}")
                },
                SkillType.SkillUITude => job switch
                {
                    0 => typeof(BaseSkill),
                    1 => typeof(TudeClawSkill),
                    2 => typeof(TudeGauntletsSkill),
                    3 => typeof(TudeDemonHandsSkill),
                    4 => typeof(NotUsedSkill),
                    _ => throw new ArgumentException($"Invalid job for Tude : {job}")
                },
                SkillType.SkillUINatasha => job switch
                {
                    0 => typeof(BaseSkill),
                    1 => typeof(NatashaRevolverSkill),
                    2 => typeof(NatashaMusketSkill),
                    3 => typeof(NatashaWeaponBagSkill),
                    4 => typeof(NotUsedSkill),
                    _ => throw new ArgumentException($"Invalid job for Natasha : {job}")
                },
                _ => throw new ArgumentException($"Invalid SkillType: {skillType}")
            } ?? throw new ArgumentException("Invalid combination of SkillType and Job");

            if (Enum.IsDefined(skillEnumType, skillClass))
            {
                var enumValue = (Enum)Enum.ToObject(skillEnumType, skillClass);
                return GetEnumDescription(enumValue);
            }

            throw new ArgumentException("Invalid Skill Class for selected Skill Tree Type and Focus");
        }

        public static List<NameID> GetSkillClassList(SkillType skillType, int job)
        {
            return skillType switch
            {
                SkillType.SkillUIFrantz => job switch
                {
                    0 => GetEnumItems<BaseSkill>(false),
                    1 => GetEnumItems<FrantzSwordSkill>(false),
                    2 => GetEnumItems<FrantzAxeSkill>(false),
                    3 => GetEnumItems<FrantzTwinSwordSkill>(false),
                    4 => GetEnumItems<NotUsedSkill>(false),
                    _ => throw new ArgumentException($"Invalid job for Frantz : {job}")
                },
                SkillType.SkillUIAngela => job switch
                {
                    0 => GetEnumItems<BaseSkill>(false),
                    1 => GetEnumItems<AngelaMagicSwordSkill>(false),
                    2 => GetEnumItems<AngelaScytleSwordSkill>(false),
                    3 => GetEnumItems<AngelaGlaiveSkill>(false),
                    4 => GetEnumItems<NotUsedSkill>(false),
                    _ => throw new ArgumentException($"Invalid job for Angela : {job}")
                },
                SkillType.SkillUITude => job switch
                {
                    0 => GetEnumItems<BaseSkill>(false),
                    1 => GetEnumItems<TudeClawSkill>(false),
                    2 => GetEnumItems<TudeGauntletsSkill>(false),
                    3 => GetEnumItems<TudeDemonHandsSkill>(false),
                    4 => GetEnumItems<NotUsedSkill>(false),
                    _ => throw new ArgumentException($"Invalid job for Tude : {job}")
                },
                SkillType.SkillUINatasha => job switch
                {
                    0 => GetEnumItems<BaseSkill>(false),
                    1 => GetEnumItems<NatashaRevolverSkill>(false),
                    2 => GetEnumItems<NatashaMusketSkill>(false),
                    3 => GetEnumItems<NatashaWeaponBagSkill>(false),
                    4 => GetEnumItems<NotUsedSkill>(false),
                    _ => throw new ArgumentException($"Invalid job for Natasha : {job}")
                },
                _ => throw new ArgumentException($"Invalid SkillType : {skillType}"),
            };
        }

        #endregion

    }
}
