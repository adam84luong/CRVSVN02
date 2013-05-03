using CardLab.CMS.PayjrSites.Interfaces;
using Common.Types;
using Common.Util;
using Payjr.GCL.Validation;
using System;

namespace CMSApp.Controls.Buxx.Account
{
    public partial class AddCreditCard : BuxxBaseControl, ICreditCardInfo
    {
        #region Properties

        public string CardNumber
        {
            get
            {
                return _cardNumberTextBox.Text;
            }
            set
            {
                _cardNumberTextBox.Text = value;
            }
        }

        public string NameOnCard
        {
            get
            {
                return _nameOnCardTextBox.Text;
            }
            set
            {
                _nameOnCardTextBox.Text = value;
            }
        }

        public DateTime ExpirationDate
        {
            get
            {
                if (_expirationDatePicker.SelectedDate.HasValue)
                {
                    return _expirationDatePicker.SelectedDate.Value;
                }
                return DateTime.MinValue;
            }
            set
            {
                _expirationDatePicker.SelectedDate = value;
            }
        }

        public string CVVSecurityCode
        {
            get
            {
                return _CVVTextBox.Text;
            }
            set
            {
                _CVVTextBox.Text = value;
            }
        }

        public CreditCardType Type { get; set; }

     
        public string ValidationGroup
        {
            set
            {
                _cardNumberRequestValidator.ValidationGroup = value;
                _cardNumberIconRequestValidator.ValidationGroup = value;
                _cardNumberExpressionValidator.ValidationGroup = value;
                _cardNumberIconExpressionValidator.ValidationGroup = value;

                _nameOnCardRequestValidator.ValidationGroup = value;
                _nameOnCardIconRequestValidator.ValidationGroup = value;
                _nameOnCardExpressionValidator.ValidationGroup = value;
                _nameOnCardIconExpressionValidator.ValidationGroup = value;

                _expirationDateRequestValidator.ValidationGroup = value;
                _expirationDateIconRequestValidator.ValidationGroup = value;

                _CVVRequestValidator.ValidationGroup = value;
                _CVVIconRequestValidator.ValidationGroup = value;
                _CVVExpressionValidator.ValidationGroup = value;
                _CVVIconExpressionValidator.ValidationGroup = value;
            }
        }

        #endregion


        #region Page Event

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            _cardNumberExpressionValidator.ValidationExpression = GCLValidator.CreditOrDebitCardRegularExpression;
            _cardNumberIconExpressionValidator.ValidationExpression = GCLValidator.CreditOrDebitCardRegularExpression;
            _nameOnCardExpressionValidator.ValidationExpression = GCLValidator.NameRegularExpression;
            _nameOnCardIconExpressionValidator.ValidationExpression = GCLValidator.NameRegularExpression;
            _CVVExpressionValidator.ValidationExpression = GCLValidator.CvvRegularExpression;
            _CVVIconExpressionValidator.ValidationExpression = GCLValidator.CvvRegularExpression;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            //CardType
            GetCreditCardTypeFromNumber(_cardNumberTextBox.Text);
        }

        #endregion


        #region Helper Method

        private void GetCreditCardTypeFromNumber(string cardNumber)
        {
            var cardType = CreditCardUtility.GetCardTypeFromNumber(cardNumber);
            if (cardType != null)
            {
                switch (cardType.Value)
                {
                    case CreditCardTypeType.Visa:
                        Type = CreditCardType.VISA;
                        break;
                    case CreditCardTypeType.MasterCard:
                        Type = CreditCardType.MASTERCARD;
                        break;
                    case CreditCardTypeType.Amex:
                        Type = CreditCardType.AMEX;
                        break;
                }
            }
        }

        #endregion

    }
}