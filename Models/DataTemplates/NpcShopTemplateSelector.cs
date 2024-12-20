﻿using System.Windows.Controls;
using static RHToolkit.Models.EnumService;

namespace RHToolkit.Models.DataTemplates
{
    public class NpcShopTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? NpcShop { get; set; }
        public DataTemplate? TradeShop { get; set; }
        public DataTemplate? ItemMix { get; set; }
        public DataTemplate? CostumeMix { get; set; }
        public DataTemplate? ShopItemVisibleFilter { get; set; }
        public DataTemplate? ItemPreview { get; set; }
        public DataTemplate? ItemBroken { get; set; }

        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        {
            return item switch
            {
                NpcShopType.NpcShop => NpcShop,
                NpcShopType.TradeShop => TradeShop,
                NpcShopType.ItemMix => ItemMix,
                NpcShopType.CostumeMix => CostumeMix,
                NpcShopType.ShopItemVisibleFilter => ShopItemVisibleFilter,
                NpcShopType.ItemPreview => ItemPreview,
                NpcShopType.ItemBroken => ItemBroken,
                _ => null,
            };
        }
    }

}
