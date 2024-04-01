using RHGMTool.Models;
using RHGMTool.Services;
using System.ComponentModel;
using System.Data;
using System.Runtime.CompilerServices;
using System.Windows;
using static RHGMTool.Utilities.EnumMapper;

namespace RHGMTool.ViewModels
{
    public class GearFrameViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private readonly SqLiteDatabaseService _databaseService;
        private readonly GMDbService _gMDbService;

        public GearFrameViewModel()
        {
            _databaseService = new SqLiteDatabaseService();
            _gMDbService = new GMDbService(_databaseService);
            InitializeItemDataTables();
            PopulateOptionItems();
        }

        #region Datatables
        private void InitializeItemDataTables()
        {
            ItemDataTable.CachedItemDataTable = GetCachedDataTable();
        }

        private DataTable? cachedDataTable;
        public DataTable? GetCachedDataTable()
        {
            if (cachedDataTable == null)
            {
                InitializeCachedDataTable();
            }

            return cachedDataTable;
        }

        private void InitializeCachedDataTable()
        {
            cachedDataTable = new DataTable();
            cachedDataTable.Columns.Add("ItemType", typeof(ItemType)); // Add a column for ItemType
            cachedDataTable = _gMDbService.CreateCachedItemDataTable(cachedDataTable, ItemType.Item, "itemlist");
            //cachedDataTable = _gMDbService.CreateCachedItemDataTable(cachedDataTable, ItemType.Costume, "itemlist_costume");
            //cachedDataTable = _gMDbService.CreateCachedItemDataTable(cachedDataTable, ItemType.Armor, "itemlist_armor");
            //cachedDataTable = _gMDbService.CreateCachedItemDataTable(cachedDataTable, ItemType.Weapon, "itemlist_weapon");
        }
        #endregion

        #region Options
        private List<NameID>? _optionItems;
        public List<NameID>? OptionItems
        {
            get { return _optionItems; }
            set
            {
                if (_optionItems != value)
                {
                    _optionItems = value;
                    OnPropertyChanged(nameof(OptionItems));
                }
            }
        }

        private void PopulateOptionItems()
        {
            try
            {
                OptionItems = new List<NameID>(_gMDbService.GetOptionItems());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region GearData

        private ItemData? _gear;

        public ItemData? Gear
        {
            get { return _gear; }
            set
            {
                _gear = value;
                OnPropertyChanged();
                UpdateGearData();
            }
        }

        private void UpdateGearData()
        {
            if (Gear != null)
            {
                // Update UI elements based on Gear data
                ItemName = Gear.Name;
                ItemNameColor = FrameData.GetBranchColor(Gear.Branch);
                Category = _gMDbService.GetCategoryName(Gear.Category);
                SubCategory = _gMDbService.GetSubCategoryName(Gear.SubCategory);
                UpdateMainStat();
                SellValue = Gear.SellPrice > 0 ? $"{Gear.SellPrice:N0} Gold" : "";
                RequiredLevel = $"Required Level: {Gear.LevelLimit}";
                ItemTrade = Gear.ItemTrade == 0 ? "Trade Unavailable" : "";
                Durability = Gear.Durability > 0 ? $"Durability: {Gear.Durability / 100}/{Gear.Durability / 100}" : "";
                JobClass = Gear.JobClass > 0 ? GetEnumDescription((CharClass)Gear.JobClass) : "";
                Weight = Gear.Weight > 0 ? $"{Gear.Weight / 1000.0:0.000}Kg" : "";
                SetName = Gear.SetId != 0 ? _gMDbService.GetSetName(Gear.SetId) : "";
                ReconstructionMax = Gear.ReconstructionMax > 0 ? $"Attribute Item ({Gear.ReconstructionMax} Times/{Gear.ReconstructionMax} Times)" : "";
                FixedBuff = $"[Fixed Buff]";
                RandomBuff = $"[Random Buff]";
                (FixedBuff01, FixedBuff01Color) = GetOptionName(Gear.FixOption00, Gear.FixOptionValue00);
                (FixedBuff02, FixedBuff02Color) = GetOptionName(Gear.FixOption01, Gear.FixOptionValue01);

                IsFixedBuffVisible = Gear.FixOption00 != 0 && Gear.FixOption01 != 0;
                IsFixedBuff01Visible = Gear.FixOption00 != 0;
                IsFixedBuff02Visible = Gear.FixOption01 != 0;

                PetFood = Gear.PetFood == 0 ? "This item cannot be used as Pet Food" : "This item can be used as Pet Food";
                PetFoodColor = Gear.PetFood == 0 ? "#e75151" : "#eed040";

                // Raise PropertyChanged for all properties
                OnPropertyChanged(nameof(Category));
                OnPropertyChanged(nameof(SubCategory));
                OnPropertyChanged(nameof(MainStat1));
                OnPropertyChanged(nameof(MainStat2));
                OnPropertyChanged(nameof(SellValue));
                OnPropertyChanged(nameof(RequiredLevel));
                OnPropertyChanged(nameof(ItemTrade));
                OnPropertyChanged(nameof(Durability));
                OnPropertyChanged(nameof(JobClass));
                OnPropertyChanged(nameof(Weight));
                OnPropertyChanged(nameof(SetName));
                OnPropertyChanged(nameof(ReconstructionMax));
                OnPropertyChanged(nameof(FixedBuff));
                OnPropertyChanged(nameof(FixedBuff01));
                OnPropertyChanged(nameof(FixedBuff02));
                OnPropertyChanged(nameof(FixedBuff01Color));
                OnPropertyChanged(nameof(FixedBuff02Color));
                OnPropertyChanged(nameof(RandomBuff));
                OnPropertyChanged(nameof(PetFood));
                OnPropertyChanged(nameof(PetFoodColor));
            }

        }

        public (string option, string color) GetOptionName(int option, int optionValue)
        {
            string fixedOption = _gMDbService.GetOptionName(option);
            (int secTime, float value, int maxValue) = _gMDbService.GetOptionValues(option);

            string colorHex = FrameData.GetColorFromOption(fixedOption);
            string formattedOption = FrameData.FormatNameID(fixedOption, $"{optionValue}", $"{secTime}", $"{value}", maxValue);

            return (formattedOption, colorHex);
        }

        private void UpdateMainStat()
        {
            if (Gear != null)
            {
                if (Gear.Type == "Armor")
                {
                    MainStat1 = $"Physical Defense +{Gear.Defense}";
                    MainStat2 = $"Magic Defense +{Gear.MagicDefense}";
                }
                else if (Gear.Type == "Weapon")
                {
                    (int PhysicalAttackMin, int PhysicalAttackMax, int MagicAttackMin, int MagicAttackMax) = _gMDbService.GetWeaponStats(Gear.JobClass, Gear.WeaponID00);
                    MainStat1 = $"Physical Damage +{PhysicalAttackMin}~{PhysicalAttackMax}";
                    MainStat2 = $"Magic Damage +{MagicAttackMin}~{MagicAttackMax}";
                }
                else
                {
                    MainStat1 = "";
                    MainStat2 = "";
                }
            }
        }

        public void UpdateSocketCount(int count)
        {
            SocketCount = $"Socket: {count}";
        }

        #endregion

        // Properties for UI elements
        public string? Category { get; private set; }
        public string? SubCategory { get; private set; }
        public string? MainStat1 { get; private set; }
        public string? MainStat2 { get; private set; }
        public string? SellValue { get; private set; }
        public string? RequiredLevel { get; private set; }
        public string? ItemTrade { get; private set; }
        public string? ReconstructionMax { get; set; }
        public string? JobClass { get; private set; }
        public string? Weight { get; set; }
        public string? SetName { get; private set; }
        public string? PetFood { get; private set; }
        public string? PetFoodColor { get; private set; }
        public string? FixedBuff { get; set; }
        public string? FixedBuff01 { get; set; }
        public string? FixedBuff02 { get; set; }
        public string? FixedBuff01Color { get; set; }
        public string? FixedBuff02Color { get; set; }
        public string? RandomBuff { get; set; }
        public string? Description { get; private set; }
        public string? Type { get; set; }
        public int WeaponID00 { get; private set; }

        public string? Durability { get; set; }
        public string? Reconstruction { get; set; }
        public int SocketCountMin { get; set; }
        public int OptionCountMin { get; set; }
        public int OptionCountMax { get; set; }


        private string? _itemName;

        public string? ItemName
        {
            get { return _itemName; }
            set
            {
                _itemName = value;
                OnPropertyChanged(nameof(ItemName));
            }
        }

        private string? _itemNameColor;

        public string? ItemNameColor
        {
            get { return _itemNameColor; }
            set
            {
                _itemNameColor = value;
                OnPropertyChanged(nameof(ItemNameColor));
            }
        }

        private string? _socketCount;
        public string? SocketCount
        {
            get { return _socketCount; }
            private set
            {
                _socketCount = value;
                OnPropertyChanged(nameof(SocketCount));
            }
        }

        private int _rankValue;
        public int RankValue
        {
            set
            {
                if (_rankValue != value)
                {
                    _rankValue = value;
                    OnPropertyChanged(nameof(RankValue));
                    Rank = FrameData.GetRankText(value);
                }
            }
        }

        private string? _rank;
        public string? Rank
        {
            get { return _rank; }
            private set
            {
                _rank = value;
                OnPropertyChanged(nameof(Rank));
            }
        }

        private int _enchantLevel;

        public int EnchantLevel
        {
            get { return _enchantLevel; }
            set
            {
                if (_enchantLevel != value)
                {
                    _enchantLevel = value;
                    OnPropertyChanged(nameof(EnchantLevel));

                }
            }
        }

        private bool _isFixedBuffVisible = true;
        public bool IsFixedBuffVisible
        {
            get { return _isFixedBuffVisible; }
            set
            {
                if (_isFixedBuffVisible != value)
                {
                    _isFixedBuffVisible = value;
                    OnPropertyChanged(nameof(IsFixedBuffVisible));
                }
            }
        }

        private bool _isFixedBuff01Visible = true;
        public bool IsFixedBuff01Visible
        {
            get { return _isFixedBuff01Visible; }
            set
            {
                if (_isFixedBuff01Visible != value)
                {
                    _isFixedBuff01Visible = value;
                    OnPropertyChanged(nameof(IsFixedBuff01Visible));
                }
            }
        }

        private bool _isFixedBuff02Visible = true;
        public bool IsFixedBuff02Visible
        {
            get { return _isFixedBuff02Visible; }
            set
            {
                if (_isFixedBuff02Visible != value)
                {
                    _isFixedBuff02Visible = value;
                    OnPropertyChanged(nameof(IsFixedBuff02Visible));
                }
            }
        }

        private int _randomOption01;
        public int RandomOption01
        {
            get { return _randomOption01; }
            set
            {
                if (_randomOption01 != value)
                {
                    _randomOption01 = value;
                    OnPropertyChanged(nameof(RandomOption01));
                    UpdateRandomBuff();
                    UpdateRandomBuffVisibility();
                }
            }
        }

        private int _randomOption01Value;
        public int RandomOption01Value
        {
            get { return _randomOption01Value; }
            set
            {
                if (_randomOption01Value != value)
                {
                    _randomOption01Value = value;
                    OnPropertyChanged(nameof(RandomOption01Value));
                    UpdateRandomBuff();
                }
            }
        }

        private int _randomOption02;
        public int RandomOption02
        {
            get { return _randomOption02; }
            set
            {
                if (_randomOption02 != value)
                {
                    _randomOption02 = value;
                    OnPropertyChanged(nameof(RandomOption02));
                    UpdateRandomBuff();
                    UpdateRandomBuffVisibility();
                    UpdateRandomBuffValue();
                }
            }
        }

        private int _randomOption02Value;
        public int RandomOption02Value
        {
            get { return _randomOption02Value; }
            set
            {
                if (_randomOption02Value != value)
                {
                    _randomOption02Value = value;
                    OnPropertyChanged(nameof(RandomOption02Value));
                    UpdateRandomBuff();
                }
            }
        }

        private int _randomOption03;
        public int RandomOption03
        {
            get { return _randomOption03; }
            set
            {
                if (_randomOption03 != value)
                {
                    _randomOption03 = value;
                    OnPropertyChanged(nameof(RandomOption03));
                    UpdateRandomBuff();
                    UpdateRandomBuffVisibility();
                }
            }
        }

        private int _randomOption03Value;
        public int RandomOption03Value
        {
            get { return _randomOption03Value; }
            set
            {
                if (_randomOption03Value != value)
                {
                    _randomOption03Value = value;
                    OnPropertyChanged(nameof(RandomOption03Value));
                    UpdateRandomBuff();
                }
            }
        }

        private string? _randomBuff01;
        public string? RandomBuff01
        {
            get { return _randomBuff01; }
            set
            {
                _randomBuff01 = value;
                OnPropertyChanged(nameof(RandomBuff01));
            }
        }

        private string? _randomBuff02;
        public string? RandomBuff02
        {
            get { return _randomBuff02; }
            set
            {
                _randomBuff02 = value;
                OnPropertyChanged(nameof(RandomBuff02));
            }
        }

        private string? _randomBuff03;
        public string? RandomBuff03
        {
            get { return _randomBuff03; }
            set
            {
                _randomBuff03 = value;
                OnPropertyChanged(nameof(RandomBuff03));
            }
        }

        private string? _randomBuff01Color;
        public string? RandomBuff01Color
        {
            get { return _randomBuff01Color; }
            set
            {
                _randomBuff01Color = value;
                OnPropertyChanged(nameof(RandomBuff01Color));
            }
        }

        private string? _randomBuff02Color;
        public string? RandomBuff02Color
        {
            get { return _randomBuff02Color; }
            set
            {
                _randomBuff02Color = value;
                OnPropertyChanged(nameof(RandomBuff02Color));
            }
        }

        private string? _randomBuff03Color;
        public string? RandomBuff03Color
        {
            get { return _randomBuff03Color; }
            set
            {
                _randomBuff03Color = value;
                OnPropertyChanged(nameof(RandomBuff03Color));
            }
        }

        private bool _isRandomBuffVisible = true;
        public bool IsRandomBuffVisible
        {
            get { return _isRandomBuffVisible; }
            set
            {
                if (_isRandomBuffVisible != value)
                {
                    _isRandomBuffVisible = value;
                    OnPropertyChanged(nameof(IsRandomBuffVisible));
                }
            }
        }

        private bool _isRandomBuff01Visible = true;
        public bool IsRandomBuff01Visible
        {
            get { return _isRandomBuff01Visible; }
            set
            {
                if (_isRandomBuff01Visible != value)
                {
                    _isRandomBuff01Visible = value;
                    OnPropertyChanged(nameof(IsRandomBuff01Visible));
                }
            }
        }

        private bool _isRandomBuff02Visible = true;
        public bool IsRandomBuff02Visible
        {
            get { return _isRandomBuff02Visible; }
            set
            {
                if (_isRandomBuff02Visible != value)
                {
                    _isRandomBuff02Visible = value;
                    OnPropertyChanged(nameof(IsRandomBuff02Visible));
                }
            }
        }

        private bool _isRandomBuff03Visible = true;
        public bool IsRandomBuff03Visible
        {
            get { return _isRandomBuff03Visible; }
            set
            {
                if (_isRandomBuff03Visible != value)
                {
                    _isRandomBuff03Visible = value;
                    OnPropertyChanged(nameof(IsRandomBuff03Visible));
                }
            }
        }

        private int _randomOption01MinValue;
        public int RandomOption01MinValue
        {
            get { return _randomOption01MinValue; }
            set
            {
                if (_randomOption01MinValue != value)
                {
                    _randomOption01MinValue = value;
                    OnPropertyChanged(nameof(RandomOption01MinValue));
                }
            }
        }

        private int _randomOption01MaxValue;
        public int RandomOption01MaxValue
        {
            get { return _randomOption01MaxValue; }
            set
            {
                if (_randomOption01MaxValue != value)
                {
                    _randomOption01MaxValue = value;
                    OnPropertyChanged(nameof(RandomOption01MaxValue));
                }
            }
        }

        private void UpdateRandomBuffVisibility()
        {
            IsRandomBuffVisible = OptionCountMax > 0;
            IsRandomBuff01Visible = RandomOption01 != 0 && OptionCountMax > 0;
            IsRandomBuff02Visible = RandomOption02 != 0 && OptionCountMax > 1;
            IsRandomBuff03Visible = RandomOption03 != 0 && OptionCountMax > 2;
        }

        private void UpdateRandomBuff()
        {
            (RandomBuff01, RandomBuff01Color) = RandomOption01 != 0 ? GetOptionName(RandomOption01, RandomOption01Value) : ("No Buff", "White");
            (RandomBuff02, RandomBuff02Color) = RandomOption02 != 0 ? GetOptionName(RandomOption02, RandomOption02Value) : ("No Buff", "White");
            (RandomBuff03, RandomBuff03Color) = RandomOption03 != 0 ? GetOptionName(RandomOption03, RandomOption03Value) : ("No Buff", "White");
        }

        private void UpdateRandomBuffValue()
        {
            (RandomOption01MinValue, RandomOption01MaxValue) = _gMDbService.GetOptionValue(RandomOption01);
        }

        private string? _socketBuff;
        public string? SocketBuff
        {
            get { return _socketBuff; }
            set
            {
                _socketBuff = value;
                OnPropertyChanged(nameof(SocketBuff));
            }
        }

        private int _socketCountMax;
        public int SocketCountMax
        {
            get { return _socketCountMax; }
            set
            {
                if (_socketCountMax != value)
                {
                    _socketCountMax = value;
                    OnPropertyChanged(nameof(SocketCountMax));
                    UpdateSocketBuffVisibility();
                    UpdateSocketBuff();
                    SocketBuff = $"Socket: {value}";
                }
            }
        }

        private int _socketOption01;
        public int SocketOption01
        {
            get { return _socketOption01; }
            set
            {
                if (_socketOption01 != value)
                {
                    _socketOption01 = value;
                    OnPropertyChanged(nameof(SocketOption01));
                    UpdateSocketBuff();
                    UpdateSocketBuffVisibility();
                }
            }
        }

        private int _socketOption01Value;
        public int SocketOption01Value
        {
            get { return _socketOption01Value; }
            set
            {
                if (_socketOption01Value != value)
                {
                    _socketOption01Value = value;
                    OnPropertyChanged(nameof(SocketOption01Value));
                    UpdateSocketBuff();
                }
            }
        }

        private int _socketOption02;
        public int SocketOption02
        {
            get { return _socketOption02; }
            set
            {
                if (_socketOption02 != value)
                {
                    _socketOption02 = value;
                    OnPropertyChanged(nameof(SocketOption02));
                    UpdateSocketBuff();
                    UpdateSocketBuffVisibility();
                }
            }
        }

        private int _socketOption02Value;
        public int SocketOption02Value
        {
            get { return _socketOption02Value; }
            set
            {
                if (_socketOption02Value != value)
                {
                    _socketOption02Value = value;
                    OnPropertyChanged(nameof(SocketOption02Value));
                    UpdateSocketBuff();
                }
            }
        }

        private int _socketOption03;
        public int SocketOption03
        {
            get { return _socketOption03; }
            set
            {
                if (_socketOption03 != value)
                {
                    _socketOption03 = value;
                    OnPropertyChanged(nameof(SocketOption03));
                    UpdateSocketBuff();
                    UpdateSocketBuffVisibility();
                }
            }
        }

        private int _socketOption03Value;
        public int SocketOption03Value
        {
            get { return _socketOption03Value; }
            set
            {
                if (_socketOption03Value != value)
                {
                    _socketOption03Value = value;
                    OnPropertyChanged(nameof(SocketOption03Value));
                    UpdateSocketBuff();
                }
            }
        }

        private string? _socketBuff01;
        public string? SocketBuff01
        {
            get { return _socketBuff01; }
            set
            {
                _socketBuff01 = value;
                OnPropertyChanged(nameof(SocketBuff01));
            }
        }

        private string? _socketBuff02;
        public string? SocketBuff02
        {
            get { return _socketBuff02; }
            set
            {
                _socketBuff02 = value;
                OnPropertyChanged(nameof(SocketBuff02));
            }
        }

        private string? _socketBuff03;
        public string? SocketBuff03
        {
            get { return _socketBuff03; }
            set
            {
                _socketBuff03 = value;
                OnPropertyChanged(nameof(SocketBuff03));
            }
        }

        private string? _socketBuff01Color;
        public string? SocketBuff01Color
        {
            get { return _socketBuff01Color; }
            set
            {
                _socketBuff01Color = value;
                OnPropertyChanged(nameof(SocketBuff01Color));
            }
        }

        private string? _socketBuff02Color;
        public string? SocketBuff02Color
        {
            get { return _socketBuff02Color; }
            set
            {
                _socketBuff02Color = value;
                OnPropertyChanged(nameof(SocketBuff02Color));
            }
        }

        private string? _socketBuff03Color;
        public string? SocketBuff03Color
        {
            get { return _socketBuff03Color; }
            set
            {
                _socketBuff03Color = value;
                OnPropertyChanged(nameof(SocketBuff03Color));
            }
        }

        private bool _isSocketBuffVisible = true;
        public bool IsSocketBuffVisible
        {
            get { return _isSocketBuffVisible; }
            set
            {
                if (_isSocketBuffVisible != value)
                {
                    _isSocketBuffVisible = value;
                    OnPropertyChanged(nameof(IsSocketBuffVisible));
                }
            }
        }

        private bool _isSocketBuff01Visible = true;
        public bool IsSocketBuff01Visible
        {
            get { return _isSocketBuff01Visible; }
            set
            {
                if (_isSocketBuff01Visible != value)
                {
                    _isSocketBuff01Visible = value;
                    OnPropertyChanged(nameof(IsSocketBuff01Visible));
                }
            }
        }

        private bool _isSocketBuff02Visible = true;
        public bool IsSocketBuff02Visible
        {
            get { return _isSocketBuff02Visible; }
            set
            {
                if (_isSocketBuff02Visible != value)
                {
                    _isSocketBuff02Visible = value;
                    OnPropertyChanged(nameof(IsSocketBuff02Visible));
                }
            }
        }

        private bool _isSocketBuff03Visible = true;
        public bool IsSocketBuff03Visible
        {
            get { return _isSocketBuff03Visible; }
            set
            {
                if (_isSocketBuff03Visible != value)
                {
                    _isSocketBuff03Visible = value;
                    OnPropertyChanged(nameof(IsSocketBuff03Visible));
                }
            }
        }

        private int _socket01Color;
        public int Socket01Color
        {
            get { return _socket01Color; }
            set
            {
                _socket01Color = value;
                OnPropertyChanged(nameof(Socket01Color));
            }
        }

        private int _socket02Color;
        public int Socket02Color
        {
            get { return _socket02Color; }
            set
            {
                _socket02Color = value;
                OnPropertyChanged(nameof(Socket02Color));
            }
        }

        private int _socket03Color;
        public int Socket03Color
        {
            get { return _socket03Color; }
            set
            {
                _socket03Color = value;
                OnPropertyChanged(nameof(Socket03Color));
            }
        }

        private void UpdateSocketBuffVisibility()
        {
            IsSocketBuffVisible = SocketCountMax > 0;
            IsSocketBuff01Visible =  SocketCountMax > 0;
            IsSocketBuff02Visible =  SocketCountMax > 1;
            IsSocketBuff03Visible = SocketCountMax > 2;
        }

        private void UpdateSocketBuff()
        {
            // Get socket buff and its color based on the SocketOption and SocketColor
            (SocketBuff01, SocketBuff01Color) = SocketOption01 != 0 ? GetOptionName(SocketOption01, SocketOption01Value) : FrameData.SetSocketColor(Socket01Color);
            (SocketBuff02, SocketBuff02Color) = SocketOption02 != 0 ? GetOptionName(SocketOption02, SocketOption02Value) : FrameData.SetSocketColor(Socket02Color);
            (SocketBuff03, SocketBuff03Color) = SocketOption03 != 0 ? GetOptionName(SocketOption03, SocketOption03Value) : FrameData.SetSocketColor(Socket03Color);
        }

    }
}
