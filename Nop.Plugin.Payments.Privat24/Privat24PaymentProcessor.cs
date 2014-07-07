using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Routing;
using Nop.Core;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Plugins;
using Nop.Plugin.Payments.Privat24.Controllers;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Payments;

namespace Nop.Plugin.Payments.Privat24
{
    public class Privat24PaymentProcessor : BasePlugin, IPaymentMethod, IPlugin
    {
        private readonly ICurrencyService _currencyService;
        private readonly CurrencySettings _currencySettings;
        private readonly HttpContextBase _httpContext;
        private readonly ILogger _logger;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IOrderService _orderService;
        private readonly PaymentSettings _paymentSettings;
        private readonly Privat24PaymentSettings _privat24PaymentSettings;
        private readonly ISettingService _settingService;
        private readonly IWebHelper _webHelper;

        public Privat24PaymentProcessor(Privat24PaymentSettings privat24PaymentSettings, 
                                        ISettingService settingService,
                                        IWebHelper webHelper, 
                                        CurrencySettings currencySettings, 
                                        ICurrencyService currencyService, 
                                        ILogger logger,
                                        PaymentSettings paymentSettings, 
                                        HttpContextBase httpContext, 
                                        IOrderService orderService,
                                        IOrderProcessingService orderProcessingService)
        {
            _privat24PaymentSettings = privat24PaymentSettings;
            _settingService = settingService;
            _webHelper = webHelper;
            _currencySettings = currencySettings;
            _currencyService = currencyService;
            _logger = logger;
            _paymentSettings = paymentSettings;
            _httpContext = httpContext;
            _orderService = orderService;
            _orderProcessingService = orderProcessingService;
        }

        public bool SupportCapture
        {
            get { return false; }
        }

        public bool SupportPartiallyRefund
        {
            get { return false; }
        }

        public bool SupportRefund
        {
            get { return false; }
        }

        public bool SupportVoid
        {
            get { return false; }
        }

        public RecurringPaymentType RecurringPaymentType
        {
            get { return RecurringPaymentType.NotSupported; }
        }

        public PaymentMethodType PaymentMethodType
        {
            get { return PaymentMethodType.Redirection; }
        }

        public bool SkipPaymentInfo
        {
            get { return false; }
        }

        public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var processPaymentResult = new ProcessPaymentResult {NewPaymentStatus = PaymentStatus.Pending};
            return processPaymentResult;
        }

        public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            string text = CreateFormPrivat24(postProcessPaymentRequest.Order.Id, postProcessPaymentRequest.Order.OrderTotal);
            LogMessage(string.Format("send data reques={0}", text));
            HttpContext.Current.Response.Redirect(text);
        }

        public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
        {
            return _privat24PaymentSettings.AdditionalFee;
        }

        public CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest)
        {
            var capturePaymentResult = new CapturePaymentResult();
            capturePaymentResult.AddError("Capture method not supported");
            return capturePaymentResult;
        }

        public RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest)
        {
            var refundPaymentResult = new RefundPaymentResult();
            refundPaymentResult.AddError("Refund method not supported");
            return refundPaymentResult;
        }

        public VoidPaymentResult Void(VoidPaymentRequest voidPaymentRequest)
        {
            var voidPaymentResult = new VoidPaymentResult();
            voidPaymentResult.AddError("Void method not supported");
            return voidPaymentResult;
        }

        public ProcessPaymentResult ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var processPaymentResult = new ProcessPaymentResult();
            processPaymentResult.AddError("Recurring payment not supported");
            return processPaymentResult;
        }

        public CancelRecurringPaymentResult CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            var cancelRecurringPaymentResult = new CancelRecurringPaymentResult();
            cancelRecurringPaymentResult.AddError("Recurring payment not supported");
            return cancelRecurringPaymentResult;
        }

        public bool CanRePostProcessPayment(Order order)
        {
            if (order == null)
            {
                throw new ArgumentNullException("order");
            }
            return false;
        }

        public void GetConfigurationRoute(out string actionName, out string controllerName,
            out RouteValueDictionary routeValues)
        {
            actionName = "Configure";
            controllerName = "PaymentPrivat24";
            routeValues = new RouteValueDictionary
            {
                { "Namespaces", "Nop.Plugin.Payments.Privat24.Controllers" },
                { "area", null }
            };
        }

        public void GetPaymentInfoRoute(out string actionName, out string controllerName,
            out RouteValueDictionary routeValues)
        {
            actionName = "PaymentInfo";
            controllerName = "PaymentPrivat24";
            routeValues = new RouteValueDictionary
            {
                { "Namespaces", "Nop.Plugin.Payments.Privat24.Controllers" },
                { "area", null }
            };
        }

        public Type GetControllerType()
        {
            return typeof (PaymentPrivat24Controller);
        }

        public override void Install()
        {
            var privat24PaymentSettings = new Privat24PaymentSettings
            {
                MerchantId = string.Empty,
                MerchantSignature = string.Empty,
                Currencies = "UAH, USD, EUR",
                AdditionalFee = 0m,
                ShowDebugInfo = false,
                IsTestMode = false
            };

            _settingService.SaveSetting(privat24PaymentSettings, 0);
           
            this.AddOrUpdatePluginLocaleResource("Nop.Payments.Privat24.MerchantId", "Merchant Id");
            this.AddOrUpdatePluginLocaleResource("Nop.Payments.Privat24.MerchantId.Hint", "Enter merchant id");
            this.AddOrUpdatePluginLocaleResource("Nop.Payments.Privat24.MerchantSignature", "merchant password");
            this.AddOrUpdatePluginLocaleResource("Nop.Payments.Privat24.MerchantSignature.Hint", "Enter merchant password.");
            this.AddOrUpdatePluginLocaleResource("Nop.Payments.Privat24.Currencies", "Enter currencies");
            this.AddOrUpdatePluginLocaleResource("Nop.Payments.Privat24.Currencies.Hint", "Separate them by comma. (USD, EUR, UAH)");
            this.AddOrUpdatePluginLocaleResource("Nop.Payments.Privat24.AdditionalFee", "Additional fee");
            this.AddOrUpdatePluginLocaleResource("Nop.Payments.Privat24.AdditionalFee.Hint", "Enter additional fee to charge your customers.");
            this.AddOrUpdatePluginLocaleResource("Nop.Payments.Privat24.ShowDebugInfo", "Enable Debugging");
            this.AddOrUpdatePluginLocaleResource("Nop.Payments.Privat24.ShowDebugInfo.Hint", "Write debugging information into file. In ~/App_Data/ folder");
            this.AddOrUpdatePluginLocaleResource("Nop.Payments.Privat24.IsTestMode", "Enable test mode");
            this.AddOrUpdatePluginLocaleResource("Nop.Payments.Privat24.IsTestMode.Hint", "Enable test mode (all transaction amounts are 1.00 USD, EUR, UAH)");
            this.AddOrUpdatePluginLocaleResource("Nop.Payments.Privat24.RedirectionTip", "You will be redirected to Privat24 site to complete the order.");
            
            base.Install();
            
            _paymentSettings.ActivePaymentMethodSystemNames.Add(PluginDescriptor.SystemName);
            _settingService.SaveSetting(_paymentSettings, 0);
        }

        public override void Uninstall()
        {
            this.DeletePluginLocaleResource("Nop.Payments.Privat24.MerchantId");
            this.DeletePluginLocaleResource("Nop.Payments.Privat24.MerchantId.Hint");
            this.DeletePluginLocaleResource("Nop.Payments.Privat24.MerchantSignature");
            this.DeletePluginLocaleResource("Nop.Payments.Privat24.MerchantSignature.Hint");
            this.DeletePluginLocaleResource("Nop.Payments.Privat24.AdditionalFee");
            this.DeletePluginLocaleResource("Nop.Payments.Privat24.AdditionalFee.Hint");
            this.DeletePluginLocaleResource("Nop.Payments.Privat24.ShowDebugInfo");
            this.DeletePluginLocaleResource("Nop.Payments.Privat24.ShowDebugInfo.Hint");
            this.DeletePluginLocaleResource("Nop.Payments.Privat24.IsTestMode");
            this.DeletePluginLocaleResource("Nop.Payments.Privat24.IsTestMode.Hint");
            this.DeletePluginLocaleResource("Nop.Payments.Privat24.RedirectionTip");

            base.Uninstall();
        }

        public void LogMessage(string message)
        {
            string systemName = PluginDescriptor.SystemName;
            if (systemName == null) throw new ArgumentNullException("systemName");
            systemName.Replace(" ", "_");
            if (systemName.Length == 0)
            {
                systemName = "privat24";
            }
            try
            {
                if (_privat24PaymentSettings.ShowDebugInfo)
                {
                    message = string.Format("{0}*******{1}{2}", DateTime.Now, Environment.NewLine, message);
                    string text2 = _httpContext.Server.MapPath("~/App_Data/" + systemName + "_log.txt");
                    try
                    {
                        if (File.Exists(text2))
                        {
                            var fileInfo = new FileInfo(text2);
                            if (fileInfo.Length > 52428800L)
                            {
                                fileInfo.Delete();
                            }
                        }
                    }
                    catch
                    {
                    }
                    using (var fileStream = new FileStream(text2, FileMode.Append, FileAccess.Write, FileShare.Read)
                        )
                    {
                        using (var streamWriter = new StreamWriter(fileStream))
                        {
                            streamWriter.WriteLine(message);
                        }
                        return;
                    }
                }
                _logger.Information("Privat24", new NopException(message), null);
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message, ex, null);
            }
        }

        public string CreateFormPrivat24(int orderid, decimal ordertotal)
        {
            string currencyCode = _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId).CurrencyCode;
            if (string.IsNullOrEmpty(currencyCode))
            {
                currencyCode = "UAH";
            }

            var currencies = _privat24PaymentSettings.Currencies.Split(new[]{",", " "}, StringSplitOptions.RemoveEmptyEntries);
            if (!currencies.Any(c => c.Equals(currencyCode, StringComparison.InvariantCultureIgnoreCase)))
            {
                currencyCode = "UAH";
            }

            string returnUrl = _webHelper.GetStoreLocation(false) + "Plugins/PaymentPrivat24/Return";
            string serverUrl = _webHelper.GetStoreLocation(false) + "Plugins/PaymentPrivat24/IPNHandler";
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendFormat("{0}", "https://api.privatbank.ua/p24api/ishop?");
            stringBuilder.AppendFormat("&amt={0}", _privat24PaymentSettings.IsTestMode ? "1.00" : ordertotal.ToString("0.00"));
            stringBuilder.AppendFormat("&ccy={0}", currencyCode);
            stringBuilder.AppendFormat("&merchant={0}", _privat24PaymentSettings.MerchantId);
            stringBuilder.AppendFormat("&order={0}", orderid);
            if (_privat24PaymentSettings.IsTestMode)
            {
                stringBuilder.AppendFormat("&details={0}", "Test mode, Demo mode");
            }
            else
            {
                stringBuilder.AppendFormat("&details={0}", "Order number=" + orderid);
            }
            stringBuilder.AppendFormat("&ext_details={0}", string.Empty);
            stringBuilder.AppendFormat("&pay_way={0}", "privat24");
            stringBuilder.AppendFormat("&return_url={0}", HttpUtility.UrlEncode(returnUrl));
            stringBuilder.AppendFormat("&server_url={0}", HttpUtility.UrlEncode(serverUrl));
            return stringBuilder.ToString();
        }

        public string Sh1(string dataString)
        {
            SHA1 sha = SHA1.Create();
            byte[] bytes = Encoding.ASCII.GetBytes(dataString);
            byte[] value = sha.ComputeHash(bytes);
            return BitConverter.ToString(value).Replace("-", string.Empty).ToLowerInvariant();
        }

        public string Md5(string password)
        {
            byte[] bytes = Encoding.Default.GetBytes(password);
            string result;
            try
            {
                var mD5CryptoServiceProvider = new MD5CryptoServiceProvider();
                byte[] array = mD5CryptoServiceProvider.ComputeHash(bytes);
                string text = string.Empty;
                byte[] array2 = array;
                for (int i = 0; i < array2.Length; i++)
                {
                    byte b = array2[i];
                    if (b < 16)
                    {
                        text = text + "0" + b.ToString("x");
                    }
                    else
                    {
                        text += b.ToString("x");
                    }
                }
                result = text;
            }
            catch
            {
                throw;
            }
            return result;
        }

        public bool ProcessCallBackRequest(string payment, string signature)
        {
            LogMessage(string.Format("payment={0}", payment));
            LogMessage(string.Format("signature={0}", signature));
            string password = payment + _privat24PaymentSettings.MerchantSignature;
            string text = Sh1(Md5(password));
            LogMessage(string.Format("signaturemy={0}", text));
            if (!string.Equals(text, signature))
            {
                LogMessage("signature!=signaturemy");
                return false;
            }
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("Privat24 IPN:");
            string order = null;
            string state = null;
            string amount = null;
            string reference = null;
            string currency = null;
            string[] array = payment.Split(new[]
            {
                '&'
            });
            foreach (string value in array)
            {
                string param = value.Trim();
                stringBuilder.AppendLine(param);
                if (param.StartsWith("order="))
                {
                    order = param.Substring(6).Trim();
                }
                if (param.StartsWith("state="))
                {
                    state = param.Substring(6).Trim();
                }
                if (param.StartsWith("amt="))
                {
                    amount = param.Substring(4).Trim();
                }
                if (param.StartsWith("ref="))
                {
                    reference = param.Substring(4).Trim();
                }
                if (param.StartsWith("ccy="))
                {
                    currency = param.Substring(4).Trim();
                }
            }
            if (state == null)
            {
                state = string.Empty;
            }
            if (reference == null)
            {
                reference = string.Empty;
            }
            if (currency == null)
            {
                currency = string.Empty;
            }
            int orderId = 0;
            int.TryParse(order, out orderId);
            Order orderById = _orderService.GetOrderById(orderId);
            if (orderById == null)
            {
                LogMessage(string.Format("bad order == null, nopOrderId={0}, nopOrderIdStr={1}", orderId, order));
                return false;
            }
            if (orderById.PaymentStatus == PaymentStatus.Paid)
            {
                LogMessage(string.Format("Order is paid, nopOrderId={0}, order.PaymentStatus={1}", orderId, orderById.PaymentStatus));
                return true;
            }

            decimal orderTotal = 0m;
            decimal.TryParse(amount, out orderTotal);
            if (_privat24PaymentSettings.IsTestMode)
            {
                orderTotal = orderById.OrderTotal;
            }
            if (orderById.OrderTotal != orderTotal)
            {
                LogMessage(string.Format("Bad OrderTotal orderid={0}, order.OrderTotal={1}, Privat24.amt={2}", orderId, orderById.OrderTotal, orderTotal));
                return false;
            }
            string currencyCode = _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId).CurrencyCode;
            if (string.IsNullOrEmpty(currencyCode))
            {
                currencyCode = "UAH";
            }

            var currencies = _privat24PaymentSettings.Currencies.Split(new[] { ",", " " }, StringSplitOptions.RemoveEmptyEntries); ;
            if (!currencies.Contains(currencyCode))
            {
                currencyCode = "UAH";
            }

            if (!string.Equals(currencyCode, currency))
            {
                LogMessage(string.Format("Bad OrderTotal currency orderid={0}, currency={1}, payment_ccy={2}", orderId, currencyCode, currency));
                return false;
            }

            ICollection<OrderNote> orderNotes = orderById.OrderNotes;
            var orderNote = new OrderNote
            {
                Note = stringBuilder.ToString(),
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow
            };
            orderNotes.Add(orderNote);
            _orderService.UpdateOrder(orderById);
            PaymentStatus paymentStatus = GetPaymentStatus(state);
            PaymentStatus paymentStatus2 = paymentStatus;
            if (paymentStatus2 <= PaymentStatus.Authorized)
            {
                if (paymentStatus2 != PaymentStatus.Pending)
                {
                    if (paymentStatus2 == PaymentStatus.Authorized)
                    {
                        if (_orderProcessingService.CanMarkOrderAsAuthorized(orderById))
                        {
                            _orderProcessingService.MarkAsAuthorized(orderById);
                        }
                    }
                }
            }
            else
            {
                if (paymentStatus2 != PaymentStatus.Paid)
                {
                    if (paymentStatus2 != PaymentStatus.Refunded)
                    {
                        if (paymentStatus2 == PaymentStatus.Voided)
                        {
                            if (_orderProcessingService.CanVoidOffline(orderById))
                            {
                                _orderProcessingService.VoidOffline(orderById);
                            }
                        }
                    }
                    else
                    {
                        if (_orderProcessingService.CanRefundOffline(orderById))
                        {
                            _orderProcessingService.RefundOffline(orderById);
                        }
                    }
                }
                else
                {
                    if (_orderProcessingService.CanMarkOrderAsPaid(orderById) &&
                        orderById.PaymentStatus != PaymentStatus.Paid)
                    {
                        _orderProcessingService.MarkOrderAsPaid(orderById);
                    }
                }
            }
            return true;
        }

        public PaymentStatus GetPaymentStatus(string paymentStatus)
        {
            switch (paymentStatus.ToLowerInvariant())
            {
                case "ok":
                case "test":
                    return PaymentStatus.Paid;
                case "wait":
                    return PaymentStatus.Pending;
                case "fail":
                    return PaymentStatus.Pending;
                default:
                    return PaymentStatus.Authorized;
            }
        }
    }
}