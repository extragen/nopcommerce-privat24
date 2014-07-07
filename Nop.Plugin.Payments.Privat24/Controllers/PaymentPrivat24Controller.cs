using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Plugin.Payments.Privat24.Models;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Web.Framework.Controllers;

namespace Nop.Plugin.Payments.Privat24.Controllers
{
    public class PaymentPrivat24Controller : BasePaymentController
    {
        private readonly ICurrencyService _currencyService;
        private readonly CurrencySettings _currencySettings;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly OrderSettings _orderSettings;
        private readonly IPaymentService _paymentService;
        private readonly PaymentSettings _paymentSettings;
        private readonly Privat24PaymentSettings _p24PaymentSettings;
        private readonly ISettingService _settingService;
        private readonly IWorkContext _workContext;

        public PaymentPrivat24Controller(ISettingService settingService, 
                                        IPaymentService paymentService,
                                        IOrderProcessingService orderProcessingService,
                                        Privat24PaymentSettings privat24PaymentSettings, 
                                        PaymentSettings paymentSettings,
                                        CurrencySettings currencySettings, 
                                        ICurrencyService currencyService, 
                                        IWorkContext workContext,
                                        OrderSettings orderSettings)
        {
            _settingService = settingService;
            _paymentService = paymentService;
            _orderProcessingService = orderProcessingService;
            _p24PaymentSettings = privat24PaymentSettings;
            _paymentSettings = paymentSettings;
            _currencySettings = currencySettings;
            _currencyService = currencyService;
            _workContext = workContext;
            _orderSettings = orderSettings;
        }

        [AdminAuthorize, ChildActionOnly]
        public ActionResult Configure()
        {
            var configurationPrivat24Model = new ConfigurationModel
            {
                MerchantId = _p24PaymentSettings.MerchantId,
                MerchantSignature = _p24PaymentSettings.MerchantSignature,
                AdditionalFee = _p24PaymentSettings.AdditionalFee,
                ShowDebugInfo = _p24PaymentSettings.ShowDebugInfo,
                IsTestMode = _p24PaymentSettings.IsTestMode
            };
            
            string currencyCode = _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId).CurrencyCode;
            string[] currencies = _p24PaymentSettings.Currencies.Split(new[]{",", " "}, StringSplitOptions.RemoveEmptyEntries);
            
            configurationPrivat24Model.IsBadCurrency = !currencies.Contains(currencyCode);
            
            return View("Nop.Plugin.Payments.Privat24.Views.PaymentPrivat24.Configure", configurationPrivat24Model);
        }

        [AdminAuthorize, ChildActionOnly, HttpPost]
        public ActionResult Configure(ConfigurationModel model)
        {
            if (!ModelState.IsValid)
            {
                return Configure();
            }

            _p24PaymentSettings.MerchantId = string.IsNullOrEmpty(model.MerchantId) ? string.Empty : model.MerchantId.Trim();
            _p24PaymentSettings.MerchantSignature = string.IsNullOrEmpty(model.MerchantSignature) ? string.Empty : model.MerchantSignature.Trim();
            _p24PaymentSettings.Currencies = string.IsNullOrEmpty(model.Currencies) ? string.Empty : model.Currencies.Trim();
            _p24PaymentSettings.AdditionalFee = model.AdditionalFee;
            _p24PaymentSettings.ShowDebugInfo = model.ShowDebugInfo;
            _p24PaymentSettings.IsTestMode = model.IsTestMode;
            _settingService.SaveSetting(_p24PaymentSettings);

            return Configure();
        }

        [ChildActionOnly]
        public ActionResult PaymentInfo()
        {
            IEnumerable<ShoppingCartItem> shoppingCartItems = _workContext.CurrentCustomer.ShoppingCartItems;
            List<ShoppingCartItem> list = shoppingCartItems.Where(i => i.ShoppingCartType == ShoppingCartType.ShoppingCart).ToList();
            if (list.Count == 0)
            {
                return Content(string.Empty);
            }

            if (!_orderProcessingService.ValidateMinOrderSubtotalAmount(list))
            {
                return Content(string.Empty);
            }

            var privat24PaymentProcessor =
                _paymentService.LoadPaymentMethodBySystemName("Nop.Payments.Privat24") as Privat24PaymentProcessor;
            if (privat24PaymentProcessor == null || !privat24PaymentProcessor.IsPaymentMethodActive(_paymentSettings) ||
                !privat24PaymentProcessor.PluginDescriptor.Installed)
            {
                throw new NopException("Privat24 module cannot be loaded");
            }

            var paymentInfoModel = new PaymentInfoModel();
            if (privat24PaymentProcessor.PaymentMethodType == PaymentMethodType.Redirection)
            {
                return View("Nop.Plugin.Payments.Privat24.Views.PaymentPrivat24.PaymentInfo", paymentInfoModel);
            }

            return View("Nop.Plugin.Payments.Privat24.Views.PaymentPrivat24.PaymentInfoButton", paymentInfoModel);
        }

        public ActionResult SubmitButton()
        {
            ActionResult result;
            try
            {
                if (_workContext.CurrentCustomer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed)
                {
                    result = RedirectToRoute("Login");
                }
                else
                {
                    IEnumerable<ShoppingCartItem> shoppingCartItems = _workContext.CurrentCustomer.ShoppingCartItems;
                    List<ShoppingCartItem> list =
                        shoppingCartItems.Where(item => item.ShoppingCartType == ShoppingCartType.ShoppingCart).ToList();
                    if (list.Count == 0)
                    {
                        result = RedirectToRoute("ShoppingCart");
                    }
                    else
                    {
                        string currencyCode =
                            _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId).CurrencyCode;
                        var curencies = _p24PaymentSettings.Currencies.Split(new[] { ",", " " }, StringSplitOptions.RemoveEmptyEntries);
                        if (!curencies.Contains(currencyCode))
                        {
                            throw new ApplicationException("Privat24. currency is not supported. (only  UAH, USD, EUR)");
                        }
                        if (string.IsNullOrEmpty(_p24PaymentSettings.MerchantId))
                        {
                            throw new ApplicationException("Privat24. merchant_id is not set");
                        }
                        if (string.IsNullOrEmpty(_p24PaymentSettings.MerchantSignature))
                        {
                            throw new ApplicationException("Privat24. merchant_sig is not set");
                        }

                        var privat24PaymentProcessor = _paymentService.LoadPaymentMethodBySystemName("Nop.Payments.Privat24") as Privat24PaymentProcessor;
                        if (privat24PaymentProcessor == null ||
                            !privat24PaymentProcessor.IsPaymentMethodActive(_paymentSettings) ||
                            !privat24PaymentProcessor.PluginDescriptor.Installed)
                        {
                            throw new NopException("Privat24 module cannot be loaded");
                        }
                        
                        var processPaymentRequest = new ProcessPaymentRequest
                        {
                            PaymentMethodSystemName = "Nop.Payments.Privat24",
                            CustomerId = _workContext.CurrentCustomer.Id
                        };

                        ProcessPaymentRequest processPaymentRequest2 = processPaymentRequest;
                        PlaceOrderResult placeOrderResult = _orderProcessingService.PlaceOrder(processPaymentRequest2);
                        if (!placeOrderResult.Success)
                        {
                            privat24PaymentProcessor.LogMessage(string.Format("CreateOrder() error: {0}",
                                placeOrderResult));
                            result = RedirectToRoute("ShoppingCart");
                        }
                        else
                        {
                            Order placedOrder = placeOrderResult.PlacedOrder;
                            if (placedOrder == null)
                            {
                                privat24PaymentProcessor.LogMessage("order==null");
                                result = RedirectToRoute("ShoppingCart");
                            }
                            else
                            {
                                privat24PaymentProcessor.LogMessage("create new order: Order Number " + placedOrder.Id);
                                string text = privat24PaymentProcessor.CreateFormPrivat24(placedOrder.Id,
                                    placedOrder.OrderTotal);
                                result = new RedirectResult(text);
                            }
                        }
                    }
                }
            }
            catch (Exception arg)
            {
                result = Content("Error: " + arg);
            }
            return result;
        }

        [NonAction]
        public override IList<string> ValidatePaymentForm(FormCollection form)
        {
            return new List<string>();
        }

        [NonAction]
        public override ProcessPaymentRequest GetPaymentInfo(FormCollection form)
        {
            return new ProcessPaymentRequest();
        }

        [ValidateInput(false)]
        public ActionResult IPNHandler(FormCollection form)
        {
            var privat24PaymentProcessor =
                _paymentService.LoadPaymentMethodBySystemName("Nop.Payments.Privat24") as Privat24PaymentProcessor;
            if (privat24PaymentProcessor == null || !privat24PaymentProcessor.IsPaymentMethodActive(_paymentSettings) ||
                !privat24PaymentProcessor.PluginDescriptor.Installed)
            {
                throw new NopException("Privat24 module cannot be loaded");
            }
            string text = form["payment"];
            string text2 = form["signature"];
            if (text == null || text2 == null)
            {
                privat24PaymentProcessor.LogMessage("payment == null || signature==null");
                return RedirectToAction("Index", "Home", new RouteValueDictionary {{"area", null}});
            }
            bool flag = privat24PaymentProcessor.ProcessCallBackRequest(text, text2);
            if (flag)
            {
                return Content(string.Empty);
            }
            return RedirectToAction("Index", "Home", new RouteValueDictionary {{"area", null}});
        }

        [ValidateInput(false)]
        public ActionResult Return()
        {
            var privat24PaymentProcessor =
                _paymentService.LoadPaymentMethodBySystemName("Nop.Payments.Privat24") as Privat24PaymentProcessor;
            if (privat24PaymentProcessor == null || !privat24PaymentProcessor.IsPaymentMethodActive(_paymentSettings) ||
                !privat24PaymentProcessor.PluginDescriptor.Installed)
            {
                throw new NopException("Privat24 module cannot be loaded");
            }
            return RedirectToAction("Index", "Home", new RouteValueDictionary {{"area", null}});
        }
    }
}