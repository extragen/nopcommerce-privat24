using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Payments.Privat24.Models
{
    public class ConfigurationModel : BaseNopModel
    {
        [NopResourceDisplayName("Nop.Payments.Privat24.MerchantId")]
        public string MerchantId { get; set; }

        [NopResourceDisplayName("Nop.Payments.Privat24.MerchantSignature")]
        public string MerchantSignature { get; set; }

        [NopResourceDisplayName("Nop.Payments.Privat24.Currencies")]
        public string Currencies { get; set; }

        [NopResourceDisplayName("Nop.Payments.Privat24.AdditionalFee")]
        public decimal AdditionalFee { get; set; }

        [NopResourceDisplayName("Nop.Payments.Privat24.ShowDebugInfo")]
        public bool ShowDebugInfo { get; set; }

        [NopResourceDisplayName("Nop.Payments.Privat24.IsTestMode")]
        public bool IsTestMode { get; set; }

        public bool IsBadCurrency { get; set; }
    }
}