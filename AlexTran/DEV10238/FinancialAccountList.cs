#region Copyright PAYjr Inc. 2005-2007
//
// All rights are reserved. Reproduction in whole or in part, in any 
// form or by any means, electronic, mechanical or otherwise, is    
// prohibited  without the prior written consent of the copyright owner.
//
#endregion

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Payjr.Entity;
using Payjr.Types;

namespace Payjr.Core.FinancialAccounts
{
    /// <summary>
    /// A list of <see cref="FinancialAccount"/>s
    /// </summary>
    /// <remarks>This class wraps the generic list </remarks>
    public class FinancialAccountList<T> : ReadOnlyCollection<T> where T : IFinancialAccount
    {
        #region Fields

        //The list of specialized accounts
        private FinancialAccountList<SavingsAccount> _savingAccounts;
        private FinancialAccountList<ACHAccount> _ACHAccounts;
        private FinancialAccountList<ACHAccount> _goodACHAccounts;
        private FinancialAccountList<ACHAccount> _allACHAccounts;
        private FinancialAccountList<PrepaidCardAccount> _prepaidAccounts;
        private FinancialAccountList<ManualPaymentAccount> _manualPaymentAccounts;
        private FinancialAccountList<CreditCardAccount> _creditCardAccounts;
        private FinancialAccountList<CreditCardAccount> _goodCreditCardAccounts;
        private FinancialAccountList<CreditCardAccount> _allCreditCardAccounts;
        private FinancialAccountList<TargetAccount> _targetAccounts;

        private FinancialAccountList<IFinancialAccount> _fundingAccounts;
        #endregion //Fields

        #region Properties

        public int BlacklistedCount
        {
            get
            {
                int count = 0;
                foreach (IFinancialAccount account in this)
                {
                    if (account.IsBlackListed())
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        #endregion

        #region ACH Accounts

        /// <summary>
        /// ACH Bank Accounts
        /// </summary>
        public FinancialAccountList<ACHAccount> ACHAccounts
        {
            get
            {
                //If we don't already have a list of savings accounts then let's go ahead
                //and create the list.  We'll save it for later if we need it

                if (_ACHAccounts == null)
                {
                    _ACHAccounts = CreateList<ACHAccount>(false);
                }

                //This may still be empty
                return _ACHAccounts;
            }
        }

        /// <summary>
        /// The active ach prepaid card account.
        /// </summary>
        public ACHAccount ActiveACHAccount
        {
            get
            {
                foreach (ACHAccount account in ACHAccounts)
                {
                    if (account.IsActive)
                    {
                        return account;
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Gets the unverified ACH account.
        /// </summary>
        /// <returns></returns>
        public ACHAccount GetUnverifiedACHAccount()
        {
            foreach (ACHAccount achAccount in ACHAccounts)
            {
                if (achAccount.Status == AccountStatus.Unverified)
                {
                    return achAccount;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets all ACH accounts, including deleted accounts.
        /// </summary>
        /// <returns></returns>
        public FinancialAccountList<ACHAccount> GetAllACHAccounts()
        {
            if (_allACHAccounts == null)
            {
                _allACHAccounts = CreateList<ACHAccount>(true);
            }

            //This may still be empty
            return _allACHAccounts;
        }

        /// <summary>
        /// Gets the good ACH accounts.
        /// </summary>
        /// <value>The good ACH accounts.</value>
        public FinancialAccountList<ACHAccount> GoodACHAccounts
        {
            get
            {
                if (_goodACHAccounts == null)
                {
                    _goodACHAccounts = new FinancialAccountList<ACHAccount>(new List<ACHAccount>());
                    foreach (ACHAccount achAcct in ACHAccounts)
                    {
                        if (achAcct.CanSupportTransfer())
                        { _goodACHAccounts.AddItem(achAcct); }
                    }
                }
                return _goodACHAccounts;
            }
        }


        #endregion //ACH Accounts

        #region Credit Cards

        /// <summary>
        /// Gets the credit card accounts.
        /// </summary>
        /// <value>The credit card accounts.</value>
        public FinancialAccountList<CreditCardAccount> CreditCardAccounts
        {
            get
            {
                if (_creditCardAccounts == null)
                {
                    _creditCardAccounts = CreateList<CreditCardAccount>(false);
                }
                return _creditCardAccounts;
            }
        }

        /// <summary>
        /// Gets the active credit card account.
        /// </summary>
        /// <value>The active credit card account.</value>
        public CreditCardAccount ActiveCreditCardAccount
        {
            get
            {
                foreach (CreditCardAccount creditCardAccount in CreditCardAccounts)
                {
                    if (creditCardAccount.IsActive == true)
                    {
                        return creditCardAccount;
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Gets all ACH accounts, including deleted accounts.
        /// </summary>
        /// <returns></returns>
        public FinancialAccountList<CreditCardAccount> GetAllCreditCardAccountAccounts()
        {
            if (_allCreditCardAccounts == null)
            {
                _allCreditCardAccounts = CreateList<CreditCardAccount>(true);
            }

            //This may still be empty
            return _allCreditCardAccounts;
        }

        /// <summary>
        /// Gets the good credit card accounts.
        /// </summary>
        /// <value>The good creditcard accounts.</value>
        public FinancialAccountList<CreditCardAccount> GoodCreditCardAccounts
        {
            get
            {
                if (_goodCreditCardAccounts == null)
                {
                    _goodCreditCardAccounts = new FinancialAccountList<CreditCardAccount>(new List<CreditCardAccount>());
                    foreach (CreditCardAccount ccAcct in CreditCardAccounts)
                    {
                        if (ccAcct.CanSupportTransfer())
                        { _goodCreditCardAccounts.AddItem(ccAcct); }
                    }
                }
                return _goodCreditCardAccounts;
            }
        }

        #endregion //Credit Cards

        #region Prepaid Accounts

        /// <summary>
        /// Prepaid Card Accounts
        /// </summary>
        public FinancialAccountList<PrepaidCardAccount> PrepaidCardAccounts
        {
            get
            {
                if (_prepaidAccounts == null)
                {
                    _prepaidAccounts = CreateList<PrepaidCardAccount>(false);
                }

                //This may still be empty
                return _prepaidAccounts;
            }
        }

        /// <summary>
        /// Get the active default savings account.
        /// </summary>
        public PrepaidCardAccount ActivePrepaidCardAccount
        {
            [DebuggerStepThrough]
            get
            {
                foreach (PrepaidCardAccount account in PrepaidCardAccounts)
                {
                    if (account.IsActive)
                    {
                        return account;
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Gets the account by card number.
        /// </summary>
        /// <param name="cardNumber">The card number.</param>
        /// <returns></returns>
        public PrepaidCardAccount GetPrepaidCardAccountByCardNumber(string cardNumber)
        {
            foreach (PrepaidCardAccount card in PrepaidCardAccounts)
            {
                if (card.CardNumber == cardNumber)
                {
                    return card;
                }
            }
            return null;
        }
    

        public PrepaidCardAccount GetPrepaidCardAccountByCustomCardDesignID(Guid customCardDesignID)
        {
            foreach (PrepaidCardAccount card in PrepaidCardAccounts)
            {
                if (card.CustomCardDesignID == customCardDesignID)
                {
                    return card;
                }
            }
            return null;
        }

        #endregion //Prepaid Accounts

        #region Target Accounts

        /// <summary>
        /// Target GiftCard Accounts
        /// </summary>
        public FinancialAccountList<TargetAccount> TargetAccounts
        {
            get
            {
                if (_targetAccounts == null)
                {
                    _targetAccounts = CreateList<TargetAccount>(false);
                }

                //This may still be empty
                return _targetAccounts;
            }
        }

        /// <summary>
        /// Get the active default Target GiftCard account.
        /// </summary>
        public TargetAccount ActiveTargetCardAccount
        {
            [DebuggerStepThrough]
            get
            {
                foreach (TargetAccount account in TargetAccounts)
                {
                    if (account.IsActive)
                    {
                        return account;
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Gets the account by card number.
        /// </summary>
        /// <param name="cardNumber">The card number.</param>
        /// <returns></returns>
        public TargetAccount GetTargetAccountByCardNumber(string cardNumber)
        {
            foreach (TargetAccount card in TargetAccounts)
            {
                if (card.CardNumber == cardNumber)
                {
                    return card;
                }
            }
            return null;
        }

        #endregion //Target Accounts

        #region Savings Account

        /// <summary>
        /// The list of Savings Financial Accounts
        /// </summary>
        public FinancialAccountList<SavingsAccount> SavingsAccounts
        {
            [DebuggerStepThrough]
            get
            {
                //If we don't already have a list of savings accounts then let's go ahead
                //and create the list.  We'll save it for later if we need it

                if (_savingAccounts == null)
                {
                    _savingAccounts = CreateList<SavingsAccount>(false);
                }

                //This may still be empty
                return _savingAccounts;
            }
        }

        /// <summary>
        /// Get the active default savings account.
        /// </summary>
        public SavingsAccount ActiveSavingsAccount
        {
            [DebuggerStepThrough]
            get
            {
                foreach (SavingsAccount savingsAccount in SavingsAccounts)
                {
                    if (savingsAccount.IsActive)
                    {
                        return savingsAccount;
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Get the unverified savings account for the user.
        /// </summary>
        public SavingsAccount UnverifiedSavingsAccount
        {
            [DebuggerStepThrough]
            get
            {
                foreach (SavingsAccount savingsAccount in SavingsAccounts)
                {
                    if (savingsAccount.Status == AccountStatus.Unverified)
                    {
                        return savingsAccount;
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Return a list of all the Verified Savings Accounts for the user.
        /// </summary>
        public FinancialAccountList<SavingsAccount> VerifiedSavingsAccounts
        {
            get
            {
                List<SavingsAccount> list = new List<SavingsAccount>();
                foreach (SavingsAccount savingsAccount in SavingsAccounts)
                {
                    if (savingsAccount.Status == AccountStatus.AllowMoneyMovement)
                    {
                        list.Add(savingsAccount);
                    }
                }
                return new FinancialAccountList<SavingsAccount>(list);
            }
        }

        #endregion //Savings Account

        #region Manual Payment Accounts

        /// <summary>
        /// Gets the manual payment account.
        /// </summary>
        /// <value>The manual payment account.</value>
        public ManualPaymentAccount ManualPaymentAccount
        {
            get
            {
                //If we don't already have a list of savings accounts then let's go ahead
                //and create the list.  We'll save it for later if we need it

                if (_manualPaymentAccounts == null)
                {
                    _manualPaymentAccounts = CreateList<ManualPaymentAccount>(false);
                }

                //This may still be empty
                if (_manualPaymentAccounts.Count > 0)
                {
                    return _manualPaymentAccounts[0];
                }
                return null;
            }
        }

        #endregion //Manual Payment Accounts

        #region Funding Accounts
        /// <summary>
        /// Retrieves a <see cref="FinancialAccountList"/> containing accounts used for funding purposes.
        /// Does not include <see cref="SavingsAccount"/>s.
        /// </summary>
        public FinancialAccountList<IFinancialAccount> FundingAccounts
        {
            get
            {
                if (_fundingAccounts == null)
                {
                    _fundingAccounts = new FinancialAccountList<IFinancialAccount>(new List<IFinancialAccount>());
                    foreach (ACHAccount bankAccount in ACHAccounts)
                    {
                        _fundingAccounts.AddItem(bankAccount);
                    }
                    foreach (CreditCardAccount creditAccount in CreditCardAccounts)
                    {
                        _fundingAccounts.AddItem(creditAccount);
                    }
                }
                return _fundingAccounts;
            }
        }

        /// <summary>
        /// Gets the good funding accounts.
        /// </summary>
        /// <value>The good funding accounts.</value>
        public FinancialAccountList<IFinancialAccount> GoodFundingAccounts
        {
            get
            {
                if (_fundingAccounts == null)
                {
                    _fundingAccounts = new FinancialAccountList<IFinancialAccount>(new List<IFinancialAccount>());
                    foreach (ACHAccount bankAccount in ACHAccounts)
                    {
                        if (bankAccount.CanSupportTransfer())
                        { _fundingAccounts.AddItem(bankAccount); }
                    }
                    foreach (CreditCardAccount creditAccount in CreditCardAccounts)
                    {
                        if (creditAccount.CanSupportTransfer())
                        { _fundingAccounts.AddItem(creditAccount); }
                    }
                }
                return _fundingAccounts;
            }
        }

        /// <summary>
        /// Gets the usable funding accounts. Usable funding accounts are accounts that are good or pending
        /// </summary>
        /// <value>The usable funding accounts.</value>
        public FinancialAccountList<IFinancialAccount> UsableFundingAccounts
        {
            get
            {
                if (_fundingAccounts == null)
                {
                    _fundingAccounts = new FinancialAccountList<IFinancialAccount>(new List<IFinancialAccount>());
                    foreach (ACHAccount bankAccount in ACHAccounts)
                    {
                        if (bankAccount.CanSupportTransfer())
                        { _fundingAccounts.AddItem(bankAccount); }
                    }
                    foreach (CreditCardAccount creditAccount in CreditCardAccounts)
                    {
                        if (creditAccount.CanSupportTransfer())
                        { _fundingAccounts.AddItem(creditAccount); }
                    }
                }
                return _fundingAccounts;
            }
        }
        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="accounts"></param>
        public FinancialAccountList(List<T> accounts)
            : base(accounts)
        {

        }

        #endregion //Constructor

        #region Methods

        /// <summary>
        /// Adds the item to the list.
        /// </summary>
        /// <param name="financialAccount">The financial account.</param>
        public void AddItem(T financialAccount)
        {
            this.Items.Add(financialAccount);

            Clear();
        }

        /// <summary>
        /// Removes the given item from the list.
        /// </summary>
        /// <param name="financialAccount">The financial account.</param>
        public void RemoveItem(T financialAccount)
        {
            this.Items.Remove(financialAccount);
        }

        /// <summary>
        /// Gets the account by ID.
        /// </summary>
        /// <param name="accountID">The account ID.</param>
        /// <returns></returns>
        public T GetAccountByID(Guid accountID)
        {
            foreach (T account in this.Items)
            {
                if ((account.AccountID == accountID) &&
                   (account.GetType() != typeof(ManualPaymentAccount)))
                {
                    return account;
                }
            }
            return default(T);
        }

        /// <summary>
        /// Determines whether a user is in a pending status.
        /// A user is in pending status if the user has only one account and that account is pending
        /// </summary>
        /// <returns>
        /// 	<c>true</c> if the user is in pending status; otherwise, <c>false</c>.
        /// </returns>
        public bool IsUserInPendingStatus()
        {
            // If there is only the Manual Payment account we return false.
            if (this.Count == 1 && this[0] is ManualPaymentAccount)
                return false;

            // Now check all other remaining accounts and see if any are pending.
            // if even one is not pending the user is not considered to be pending.
            foreach (IFinancialAccount account in this)
            {
                if (!(account is ManualPaymentAccount))
                {
                    if (!account.IsAccountPending)
                        return false;
                }
            }
            // If all the non manual payment accounts are pending then the user is considered
            // to be a pending user.
            return true;
        }

        /// <summary>
        /// Create a subset of accounts based on our current accounts
        /// </summary>
        /// <typeparam name="FA">Type of Account to create a new list for.  </typeparam>
        /// <returns></returns>
        private FinancialAccountList<FA> CreateList<FA>(bool includeDeleted) where FA : IFinancialAccount
        {

            List<FA> accounts = new List<FA>(Count);

            foreach (IFinancialAccount account in this)
            {
                if (account.GetType() == typeof(FA))
                {
                    if (includeDeleted || !account.MarkedForDeletion)
                    { accounts.Add((FA)account); }
                }
            }

            return new FinancialAccountList<FA>(accounts);
        }

        /// <summary>
        /// Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1"></see>.
        /// </summary>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"></see> is read-only. </exception>
        public void Clear()
        {
            _savingAccounts = null;
            _ACHAccounts = null;
            _prepaidAccounts = null;
            _manualPaymentAccounts = null;
            _creditCardAccounts = null;
            _targetAccounts = null;

            _fundingAccounts = null;
        }

        /// <summary>
        /// Determines whether this instance [can be deleted].
        /// </summary>
        /// <param name="product">The product.</param>
        /// <returns>
        /// 	<c>true</c> if this instance [can be deleted]; otherwise, <c>false</c>.
        /// </returns>
        public bool CanDeleteFundingAccount(Product product)
        {
            switch (product)
            {
                case Product.PPaid_Standard_Branding_I:
                    //If the user only has one account between ACH and Credit card Account do not allow them to delete
                    return ((this.ACHAccounts.Count + this.CreditCardAccounts.Count) > 1);
                default:
                    return false;
            }
        }
        #endregion //Methods
    }
}
