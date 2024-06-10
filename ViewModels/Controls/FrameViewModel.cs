using RHToolkit.Properties;
using RHToolkit.Services;
using static RHToolkit.Models.EnumService;

namespace RHToolkit.ViewModels.Controls;

public partial class FrameViewModel(IFrameService frameService, IGMDatabaseService gmDatabaseService) : ObservableObject
{
    private readonly IFrameService _frameService = frameService;
    private readonly IGMDatabaseService _gmDatabaseService = gmDatabaseService;

    #region Properties

    [ObservableProperty]
    private string? _itemName;

    public string ItemNameColor => FrameService.GetBranchColor(ItemBranch);

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

    public string WeightText => FrameService.FormatWeight(Weight);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DurabilityText))]
    private int _maxDurability;

    public string DurabilityText => FrameService.FormatDurability(MaxDurability);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReconstructionText))]
    private int _reconstruction;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReconstructionText))]
    private int _reconstructionMax;

    public string ReconstructionText => FrameService.FormatReconstruction(Reconstruction, ReconstructionMax, ItemTrade);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RankText))]
    private int _rank = 1;

    public string RankText => FrameService.GetRankText(Rank);

    [ObservableProperty]
    private int _enhanceLevel;

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

    public string SellValueText => FrameService.FormatSellValue(SellPrice);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RequiredLevelText))]
    private int _requiredLevel;

    public string RequiredLevelText => FrameService.FormatRequiredLevel(RequiredLevel);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ItemTradeText))]
    private int _itemTrade;

    public string ItemTradeText => FrameService.FormatItemTrade(ItemTrade);

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

    public string PetFoodText => FrameService.FormatPetFood(PetFood);

    public string PetFoodColor => FrameService.FormatPetFoodColor(PetFood);

    public static string AugmentStone => Resources.AugmentStone;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AugmentText))]
    private int _augmentValue;

    public string AugmentText => FrameService.FormatAugmentStone(AugmentValue);

#endregion

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
    [NotifyPropertyChangedFor(nameof(RandomOption01Value))]
    private int _optionCount;


    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RandomOption01Value))]
    private int _OptionCountMax;

    public string RandomOption => Category == 29 ? $"[{Resources.Buff}]" : $"[{Resources.RandomBuff}]";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RandomOption01Text))]
    [NotifyPropertyChangedFor(nameof(RandomOption01Color))]
    private int _randomOption01;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RandomOption01Text))]
    private int _randomOption01Value;

    public string RandomOption01Text => _frameService.GetOptionName(RandomOption01, RandomOption01Value);

    public string? RandomOption01Color => _frameService.GetColorFromOption(RandomOption01);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RandomOption02Text))]
    [NotifyPropertyChangedFor(nameof(RandomOption02Color))]
    private int _randomOption02;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RandomOption02Text))]
    private int _randomOption02Value;

    public string RandomOption02Text => _frameService.GetOptionName(RandomOption02, RandomOption02Value);

    public string? RandomOption02Color => _frameService.GetColorFromOption(RandomOption02);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RandomOption03Text))]
    [NotifyPropertyChangedFor(nameof(RandomOption03Color))]
    private int _randomOption03;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RandomOption03Text))]
    private int _randomOption03Value;

    public string RandomOption03Text => _frameService.GetOptionName(RandomOption03, RandomOption03Value);

    public string? RandomOption03Color => _frameService.GetColorFromOption(RandomOption03);

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
    private int _socketOption01;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SocketOption01Text))]
    private int _socketOption01Value;

    public string SocketOption01Text => SocketOption01 != 0 ? _frameService.GetOptionName(SocketOption01, SocketOption01Value) : FrameService.GetSocketText(Socket01Color);

    public string SocketOption01Color => SocketOption01 != 0 ? _frameService.GetColorFromOption(SocketOption01) : FrameService.GetSocketColor(Socket01Color);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SocketOption02Text))]
    [NotifyPropertyChangedFor(nameof(SocketOption02Color))]
    private int _socketOption02;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SocketOption02Text))]
    private int _socketOption02Value;

    public string SocketOption02Text => SocketOption02 != 0 ? _frameService.GetOptionName(SocketOption02, SocketOption02Value) : FrameService.GetSocketText(Socket02Color);

    public string? SocketOption02Color => SocketOption02 != 0 ? _frameService.GetColorFromOption(SocketOption02) : FrameService.GetSocketColor(Socket02Color);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SocketOption03Text))]
    [NotifyPropertyChangedFor(nameof(SocketOption03Color))]
    private int _socketOption03;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SocketOption03Text))]
    private int _socketOption03Value;

    public string SocketOption03Text => SocketOption03 != 0 ? _frameService.GetOptionName(SocketOption03, SocketOption03Value) : FrameService.GetSocketText(Socket03Color);

    public string? SocketOption03Color => SocketOption03 != 0 ? _frameService.GetColorFromOption(SocketOption03) : FrameService.GetSocketColor(Socket03Color);

    #endregion
}