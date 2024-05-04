using RHToolkit.Properties;
using RHToolkit.Services;
using static RHToolkit.Models.EnumService;

namespace RHToolkit.ViewModels.Controls;

public partial class FrameViewModel(IFrameService frameService, IGMDatabaseService gmDatabaseService) : ObservableObject
{
    private readonly IFrameService _frameService = frameService;
    private readonly IGMDatabaseService _gmDatabaseService = gmDatabaseService;

    [ObservableProperty]
    private string? _itemName;

    public string ItemNameColor => _frameService.GetBranchColor(ItemBranch);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ItemNameColor))]
    private int _itemBranch;

    [ObservableProperty]
    private string? _iconName;

    [ObservableProperty]
    private int _type;

    [ObservableProperty]
    private string? _description;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WeightText))]
    private int _weight;

    public string WeightText => _frameService.FormatWeight(Weight);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DurabilityText))]
    private int _maxDurability;

    public string DurabilityText => _frameService.FormatDurability(MaxDurability);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReconstructionText))]
    private int _reconstruction;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReconstructionText))]
    private int _reconstructionMax;

    public string ReconstructionText => _frameService.FormatReconstruction(Reconstruction, ReconstructionMax, ItemTrade);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RankText))]
    private int _rank = 1;

    public string RankText => _frameService.GetRankText(Rank);

    [ObservableProperty]
    private int _enchantLevel;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CategoryText))]
    [NotifyPropertyChangedFor(nameof(RandomOption))]
    private int _category;

    public string CategoryText => _gmDatabaseService.GetCategoryName(Category);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SubCategoryText))]
    private int _subCategory;

    public string? SubCategoryText => _gmDatabaseService.GetSubCategoryName(SubCategory);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MainStatText))]
    private int _defense;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MainStatText))]
    private int _magicDefense;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MainStatText))]
    private int _weaponID00;

    public string MainStatText => _frameService.FormatMainStat(Type, Defense, MagicDefense, JobClass, WeaponID00);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SellValueText))]
    private int _sellPrice;

    public string SellValueText => _frameService.FormatSellValue(SellPrice);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RequiredLevelText))]
    private int _requiredLevel;

    public string RequiredLevelText => _frameService.FormatRequiredLevel(RequiredLevel);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ItemTradeText))]
    private int _itemTrade;

    public string ItemTradeText => _frameService.FormatItemTrade(ItemTrade);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(JobClassText))]
    private int _jobClass;

    public string JobClassText => JobClass != 0 ? GetEnumDescription((CharClass)JobClass) : "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SetNameText))]
    [NotifyPropertyChangedFor(nameof(SetEffectText))]
    private int _setId;

    public string SetNameText => _gmDatabaseService.GetSetName(SetId);
    public string SetEffectText => _frameService.FormatSetEffect(SetId);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PetFoodText))]
    [NotifyPropertyChangedFor(nameof(PetFoodColor))]
    private int _petFood;

    public string PetFoodText => _frameService.FormatPetFood(PetFood);

    public string PetFoodColor => _frameService.FormatPetFoodColor(PetFood);

    public static string AugmentStone => Resources.AugmentStone;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AugmentText))]
    private int _augmentValue;

    public string AugmentText => _frameService.FormatAugmentStone(AugmentValue);

    #region Fixed Option

    public static string FixedOption => $"[{Resources.FixedBuff}]";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FixedOption01Text))]
    [NotifyPropertyChangedFor(nameof(FixedOption01Color))]
    private int _fixedOption01;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FixedOption01Text))]
    private int _fixedOption01Value;

    public string FixedOption01Text => _frameService.GetOptionName(FixedOption01, FixedOption01Value, true);

    public string FixedOption01Color => _frameService.GetColorFromOption(FixedOption01);


    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FixedOption02Text))]
    [NotifyPropertyChangedFor(nameof(FixedOption02Color))]
    private int _fixedOption02;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FixedOption02Text))]
    private int _fixedOption02Value;

    public string FixedOption02Text => _frameService.GetOptionName(FixedOption02, FixedOption02Value, true);

    public string FixedOption02Color => _frameService.GetColorFromOption(FixedOption02);

    #endregion

    #region Random Option

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RandomOption01MinValue))]
    [NotifyPropertyChangedFor(nameof(RandomOption01MaxValue))]
    [NotifyPropertyChangedFor(nameof(RandomOption01Value))]
    private int _optionCount;


    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RandomOption01MinValue))]
    [NotifyPropertyChangedFor(nameof(RandomOption01MaxValue))]
    [NotifyPropertyChangedFor(nameof(RandomOption01Value))]
    private int _OptionCountMax;

    public string RandomOption => Category == 29 ? $"[{Resources.Buff}]" : $"[{Resources.RandomBuff}]";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RandomOption01Text))]
    [NotifyPropertyChangedFor(nameof(RandomOption01Color))]
    [NotifyPropertyChangedFor(nameof(RandomOption01MinValue))]
    [NotifyPropertyChangedFor(nameof(RandomOption01MaxValue))]
    private int _randomOption01;

    partial void OnRandomOption01Changed(int value)
    {
        (RandomOption01MinValue, RandomOption01MaxValue) = _gmDatabaseService.GetOptionValue(value);
        RandomOption01Value = CalculateOptionValue(value, RandomOption01Value, RandomOption01MaxValue);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RandomOption01Text))]
    private int _randomOption01Value;

    public string RandomOption01Text => _frameService.GetOptionName(RandomOption01, RandomOption01Value);

    public string? RandomOption01Color => _frameService.GetColorFromOption(RandomOption01);

    [ObservableProperty]
    private int _randomOption01MinValue;

    [ObservableProperty]
    private int _randomOption01MaxValue;
    partial void OnRandomOption01MaxValueChanged(int value)
    {
        if (RandomOption01Value > value)
            RandomOption01Value = value;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RandomOption02Text))]
    [NotifyPropertyChangedFor(nameof(RandomOption02Color))]
    [NotifyPropertyChangedFor(nameof(RandomOption02MinValue))]
    [NotifyPropertyChangedFor(nameof(RandomOption02MaxValue))]
    private int _randomOption02;

    partial void OnRandomOption02Changed(int value)
    {
        (RandomOption02MinValue, RandomOption02MaxValue) = _gmDatabaseService.GetOptionValue(value);
        RandomOption02Value = CalculateOptionValue(value, RandomOption02Value, RandomOption02MaxValue);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RandomOption02Text))]
    private int _randomOption02Value;

    public string RandomOption02Text => _frameService.GetOptionName(RandomOption02, RandomOption02Value);

    public string? RandomOption02Color => _frameService.GetColorFromOption(RandomOption02);

    [ObservableProperty]
    private int _randomOption02MinValue;

    [ObservableProperty]
    private int _randomOption02MaxValue;
    partial void OnRandomOption02MaxValueChanged(int value)
    {
        if (RandomOption02Value > value)
            RandomOption02Value = value;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RandomOption03Text))]
    [NotifyPropertyChangedFor(nameof(RandomOption03Color))]
    [NotifyPropertyChangedFor(nameof(RandomOption03MinValue))]
    [NotifyPropertyChangedFor(nameof(RandomOption03MaxValue))]
    private int _randomOption03;

    partial void OnRandomOption03Changed(int value)
    {
        (RandomOption03MinValue, RandomOption03MaxValue) = _gmDatabaseService.GetOptionValue(value);
        RandomOption03Value = CalculateOptionValue(value, RandomOption03Value, RandomOption03MaxValue);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RandomOption03Text))]
    private int _randomOption03Value;

    public string RandomOption03Text => _frameService.GetOptionName(RandomOption03, RandomOption03Value);

    public string? RandomOption03Color => _frameService.GetColorFromOption(RandomOption03);

    [ObservableProperty]
    private int _randomOption03MinValue;

    [ObservableProperty]
    private int _randomOption03MaxValue;
    partial void OnRandomOption03MaxValueChanged(int value)
    {
        if (RandomOption03Value > value)
            RandomOption03Value = value;
    }

    private static int CalculateOptionValue(int option, int value, int maxValue)
    {
        if (option != 0)
        {
            if (value == 0)
            {
                return maxValue;
            }
        }
        else
        {
            return 0;
        }
        return value;
    }
    #endregion

    #region Socket Option

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SocketOption))]
    private int _socketCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SocketOption))]
    private int _socketCountMax;

    public string SocketOption => $"{Resources.Socket}: {SocketCount}/{SocketCountMax}";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SocketOption01Text))]
    [NotifyPropertyChangedFor(nameof(SocketOption01Color))]
    private int _socket01Color;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SocketOption02Text))]
    [NotifyPropertyChangedFor(nameof(SocketOption02Color))]
    private int _socket02Color;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SocketOption03Text))]
    [NotifyPropertyChangedFor(nameof(SocketOption03Color))]
    private int _socket03Color;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SocketOption01Text))]
    [NotifyPropertyChangedFor(nameof(SocketOption01Color))]
    [NotifyPropertyChangedFor(nameof(SocketOption01MinValue))]
    [NotifyPropertyChangedFor(nameof(SocketOption01MaxValue))]
    private int _socketOption01;

    partial void OnSocketOption01Changed(int value)
    {
        (SocketOption01MinValue, SocketOption01MaxValue) = _gmDatabaseService.GetOptionValue(value);
        SocketOption01Value = CalculateOptionValue(value, SocketOption01Value, SocketOption01MaxValue);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SocketOption01Text))]
    private int _socketOption01Value;

    public string SocketOption01Text => SocketOption01 != 0 ? _frameService.GetOptionName(SocketOption01, SocketOption01Value) : _frameService.GetSocketText(Socket01Color);

    public string SocketOption01Color => SocketOption01 != 0 ? _frameService.GetColorFromOption(SocketOption01) : _frameService.GetSocketColor(Socket01Color);

    [ObservableProperty]
    private int _socketOption01MinValue;

    [ObservableProperty]
    private int _socketOption01MaxValue;
    partial void OnSocketOption01MaxValueChanged(int value)
    {
        if (SocketOption01Value > value)
            SocketOption01Value = value;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SocketOption02Text))]
    [NotifyPropertyChangedFor(nameof(SocketOption02Color))]
    [NotifyPropertyChangedFor(nameof(SocketOption02MinValue))]
    [NotifyPropertyChangedFor(nameof(SocketOption02MaxValue))]
    private int _socketOption02;

    partial void OnSocketOption02Changed(int value)
    {
        (SocketOption02MinValue, SocketOption02MaxValue) = _gmDatabaseService.GetOptionValue(value);
        SocketOption02Value = CalculateOptionValue(value, SocketOption02Value, SocketOption02MaxValue);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SocketOption02Text))]
    private int _socketOption02Value;

    public string SocketOption02Text => SocketOption02 != 0 ? _frameService.GetOptionName(SocketOption02, SocketOption02Value) : _frameService.GetSocketText(Socket02Color);

    public string? SocketOption02Color => SocketOption02 != 0 ? _frameService.GetColorFromOption(SocketOption02) : _frameService.GetSocketColor(Socket02Color);

    [ObservableProperty]
    private int _socketOption02MinValue;

    [ObservableProperty]
    private int _socketOption02MaxValue;
    partial void OnSocketOption02MaxValueChanged(int value)
    {
        if (SocketOption02Value > value)
            SocketOption02Value = value;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SocketOption03Text))]
    [NotifyPropertyChangedFor(nameof(SocketOption03Color))]
    [NotifyPropertyChangedFor(nameof(SocketOption03MinValue))]
    [NotifyPropertyChangedFor(nameof(SocketOption03MaxValue))]
    private int _socketOption03;

    partial void OnSocketOption03Changed(int value)
    {
        (SocketOption03MinValue, SocketOption03MaxValue) = _gmDatabaseService.GetOptionValue(value);
        SocketOption03Value = CalculateOptionValue(value, SocketOption03Value, SocketOption03MaxValue);
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SocketOption03Text))]
    private int _socketOption03Value;

    public string SocketOption03Text => SocketOption03 != 0 ? _frameService.GetOptionName(SocketOption03, SocketOption03Value) : _frameService.GetSocketText(Socket03Color);

    public string? SocketOption03Color => SocketOption03 != 0 ? _frameService.GetColorFromOption(SocketOption03) : _frameService.GetSocketColor(Socket03Color);

    [ObservableProperty]
    private int _socketOption03MinValue;

    [ObservableProperty]
    private int _socketOption03MaxValue;
    partial void OnSocketOption03MaxValueChanged(int value)
    {
        if (SocketOption03Value > value)
            SocketOption03Value = value;
    }

    #endregion
}