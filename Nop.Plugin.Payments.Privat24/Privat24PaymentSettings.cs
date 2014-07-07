using Nop.Core.Configuration;

namespace Nop.Plugin.Payments.Privat24
{
    public class Privat24PaymentSettings : ISettings
    {
        public virtual string MerchantId { get; set; }
        public virtual string MerchantSignature { get; set; }
        public virtual string Currencies { get; set; }
        public decimal AdditionalFee { get; set; }
        public bool ShowDebugInfo { get; set; }
        public bool IsTestMode { get; set; }
    }
}