/***************************************************************************** 
* Copyright 2016 Aurora Solutions 
* 
*    http://www.aurorasolutions.io 
* 
* Aurora Solutions is an innovative services and product company at 
* the forefront of the software industry, with processes and practices 
* involving Domain Driven Design(DDD), Agile methodologies to build 
* scalable, secure, reliable and high performance products.
* 
* Coin Exchange is a high performance exchange system specialized for
* Crypto currency trading. It has different general purpose uses such as
* independent deposit and withdrawal channels for Bitcoin and Litecoin,
* but can also act as a standalone exchange that can be used with
* different asset classes.
* Coin Exchange uses state of the art technologies such as ASP.NET REST API,
* AngularJS and NUnit. It also uses design patterns for complex event
* processing and handling of thousands of transactions per second, such as
* Domain Driven Designing, Disruptor Pattern and CQRS With Event Sourcing.
* 
* Licensed under the Apache License, Version 2.0 (the "License"); 
* you may not use this file except in compliance with the License. 
* You may obtain a copy of the License at 
* 
*    http://www.apache.org/licenses/LICENSE-2.0 
* 
* Unless required by applicable law or agreed to in writing, software 
* distributed under the License is distributed on an "AS IS" BASIS, 
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
* See the License for the specific language governing permissions and 
* limitations under the License. 
*****************************************************************************/


﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoinExchange.Funds.Application.WithdrawServices.Commands;
using CoinExchange.Funds.Application.WithdrawServices.Representations;
using CoinExchange.Funds.Domain.Model.BalanceAggregate;
using CoinExchange.Funds.Domain.Model.CurrencyAggregate;
using CoinExchange.Funds.Domain.Model.DepositAggregate;
using CoinExchange.Funds.Domain.Model.Repositories;
using CoinExchange.Funds.Domain.Model.Services;
using CoinExchange.Funds.Domain.Model.WithdrawAggregate;

namespace CoinExchange.Funds.Application.WithdrawServices
{
    /// <summary>
    /// Withdraw Application Service
    /// </summary>
    public class WithdrawApplicationService : IWithdrawApplicationService
    {
        // Get the Current Logger
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger
        (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private IFundsPersistenceRepository _fundsPersistenceRepository;
        private IWithdrawAddressRepository _withdrawAddressRepository;
        private IFundsValidationService _fundsValidationService;
        private IWithdrawRepository _withdrawRepository;
        private IClientInteractionService _withdrawSubmissionService;
        private IDepositAddressRepository _depositAddressRepository;
        private IWithdrawLimitRepository _withdrawLimitRepository;
        private IBalanceRepository _balanceRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Object"/> class.
        /// </summary>
        public WithdrawApplicationService(IFundsPersistenceRepository fundsPersistenceRepository, 
            IWithdrawAddressRepository withdrawAddressRepository, 
            IFundsValidationService fundsValidationService, IWithdrawRepository withdrawRepository, 
            IClientInteractionService clientInteractionService, IDepositAddressRepository depositAddressRepository, 
            IWithdrawLimitRepository withdrawLimitRepository, IBalanceRepository balanceRepository)
        {
            _fundsPersistenceRepository = fundsPersistenceRepository;
            _withdrawAddressRepository = withdrawAddressRepository;
            _fundsValidationService = fundsValidationService;
            _withdrawRepository = withdrawRepository;
            _withdrawSubmissionService = clientInteractionService;
            _depositAddressRepository = depositAddressRepository;
            _withdrawLimitRepository = withdrawLimitRepository;
            _balanceRepository = balanceRepository;

            _withdrawSubmissionService.WithdrawExecuted += this.WithdrawExecuted;
        }

        /// <summary>
        /// Invoked when a withdraw is committed to the network
        /// </summary>
        /// <param name="withdraw"></param>
        private void WithdrawExecuted(Withdraw withdraw)
        {
            Log.Debug(string.Format("Withdraw executed event received: Account ID = {0}, Currency = {1}", withdraw.AccountId.Value, withdraw.Currency.Name));
            _fundsValidationService.WithdrawalExecuted(withdraw);
        }

        /// <summary>
        /// Gets the list of recent withdrawals for an Account ID
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns></returns>
        public List<WithdrawRepresentation> GetRecentWithdrawals(int accountId)
        {
            List<WithdrawRepresentation> withdrawRepresentations = new List<WithdrawRepresentation>();
            List<Withdraw> withdrawals = _withdrawRepository.GetWithdrawByAccountId(new AccountId(accountId));
            if (withdrawals != null && withdrawals.Any())
            {
                foreach (var withdrawal in withdrawals)
                {
                    withdrawRepresentations.Add(new WithdrawRepresentation(withdrawal.Currency.Name, withdrawal.WithdrawId,
                        withdrawal.DateTime, withdrawal.Type.ToString(), withdrawal.Amount, withdrawal.Fee,
                        withdrawal.Status.ToString(), (withdrawal.BitcoinAddress == null) ? null : withdrawal.BitcoinAddress.Value,
                        (withdrawal.TransactionId == null) ? null : withdrawal.TransactionId.Value));
                }
            }

            return withdrawRepresentations;
        }

        /// <summary>
        /// Get recent withdrawals for hte given currency and account id
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="currency"></param>
        /// <returns></returns>
        public List<WithdrawRepresentation> GetRecentWithdrawals(int accountId, string currency)
        {
            List<WithdrawRepresentation> withdrawRepresentations = new List<WithdrawRepresentation>();
            List<Withdraw> withdrawals = _withdrawRepository.GetWithdrawByCurrencyAndAccountId(currency, new AccountId(accountId));
            if (withdrawals != null && withdrawals.Any())
            {
                foreach (var withdrawal in withdrawals)
                {
                    withdrawRepresentations.Add(new WithdrawRepresentation(withdrawal.Currency.Name, withdrawal.WithdrawId,
                        withdrawal.DateTime, withdrawal.Type.ToString(), withdrawal.Amount, withdrawal.Fee,
                        withdrawal.Status.ToString(), (withdrawal.BitcoinAddress == null) ? null : withdrawal.BitcoinAddress.Value, 
                        (withdrawal.TransactionId == null) ? null : withdrawal.TransactionId.Value));
                }
            }

            return withdrawRepresentations;
        }

        /// <summary>
        /// Add new Address
        /// </summary>
        /// <param name="addAddressCommand"></param>
        /// <returns></returns>
        public WithdrawAddressResponse AddAddress(AddAddressCommand addAddressCommand)
        {
            if (addAddressCommand.BitcoinAddress.Length < 26 || addAddressCommand.BitcoinAddress.Length > 34)
            {
                return new WithdrawAddressResponse(false, "Invalid address");
            }
            List<WithdrawAddress> withdrawAddresses = _withdrawAddressRepository.GetWithdrawAddressByAccountIdAndCurrency(
                new AccountId(addAddressCommand.AccountId), new Currency(addAddressCommand.Currency));
            foreach (var address in withdrawAddresses)
            {
                if (address.BitcoinAddress.Value == addAddressCommand.BitcoinAddress || 
                    address.Description == addAddressCommand.Description)
                {
                    return new WithdrawAddressResponse(false, "Duplicate Entry");
                }
            }
            // Create a new address and save in the database
            WithdrawAddress withdrawAddress = new WithdrawAddress(new Currency(addAddressCommand.Currency), 
                new BitcoinAddress(addAddressCommand.BitcoinAddress), addAddressCommand.Description,
                new AccountId(addAddressCommand.AccountId), DateTime.Now);

            _fundsPersistenceRepository.SaveOrUpdate(withdrawAddress);

            DepositAddress depositAddress = _depositAddressRepository.GetDepositAddressByAddress(new BitcoinAddress(addAddressCommand.BitcoinAddress));
            if (depositAddress != null)
            {
                depositAddress.StatusUsed();
                _fundsPersistenceRepository.SaveOrUpdate(depositAddress);
            }
            return new WithdrawAddressResponse(true, "Address Saved");
        }

        /// <summary>
        /// Gets the list of withdrawaal addresses
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="currency"> </param>
        /// <returns></returns>
        public List<WithdrawAddressRepresentation> GetWithdrawalAddresses(int accountId, string currency)
        {
            // Get the list of withdraw addresses, extract the information into the withdraw address representation list and 
            // return 
            List<WithdrawAddressRepresentation> withdrawAddressRepresentations = new List<WithdrawAddressRepresentation>();
            List<WithdrawAddress> withdrawAddresses = _withdrawAddressRepository.GetWithdrawAddressByAccountIdAndCurrency(
                new AccountId(accountId), new Currency(currency));
            foreach (var withdrawAddress in withdrawAddresses)
            {
                withdrawAddressRepresentations.Add(new WithdrawAddressRepresentation(withdrawAddress.BitcoinAddress.Value,
                    withdrawAddress.Description));
            }
            return withdrawAddressRepresentations;
        }

        /// <summary>
        /// Commit the Withdrawal from the user
        /// </summary>
        /// <param name="commitWithdrawCommand"></param>
        /// <returns></returns>
        public CommitWithdrawResponse CommitWithdrawal(CommitWithdrawCommand commitWithdrawCommand)
        {
            Balance balance =
                _balanceRepository.GetBalanceByCurrencyAndAccountId(new Currency(commitWithdrawCommand.Currency),
                                                                    new AccountId(commitWithdrawCommand.AccountId));
            if (balance != null)
            {
                if (balance.IsFrozen)
                {
                    throw new InvalidOperationException(
                        string.Format("Account balance Frozen for Account ID = {0}, Currency = {1}",
                                      commitWithdrawCommand.AccountId, commitWithdrawCommand.Currency));
                }
            }

            Withdraw withdraw = _fundsValidationService.ValidateFundsForWithdrawal(
                new AccountId(commitWithdrawCommand.AccountId), new Currency(commitWithdrawCommand.Currency,
                                                                             commitWithdrawCommand.IsCryptoCurrency),
                commitWithdrawCommand.Amount, null /*Null until confirmation of what to use*/,
                new BitcoinAddress(commitWithdrawCommand.BitcoinAddress));

            if (withdraw != null)
            {
                bool commitWithdrawResponse = _withdrawSubmissionService.CommitWithdraw(withdraw);
                return new CommitWithdrawResponse(commitWithdrawResponse, withdraw.WithdrawId, null);
            }
            throw new InvalidOperationException(string.Format("Could not commit withdraw: AccountId = {0}",
                                                              commitWithdrawCommand.AccountId));
        }

        /// <summary>
        /// Get the threshold limits for the withdrawal
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="currency"></param>
        /// <returns></returns>
        public WithdrawLimitRepresentation GetWithdrawLimitThresholds(int accountId, string currency)
        {
            // Find the total limits and calculate the threshold limits used by the user
            AccountWithdrawLimits accountWithdrawLimits = _fundsValidationService.GetWithdrawThresholds(new AccountId(accountId), new Currency(currency));
            if (accountWithdrawLimits != null)
            {
                // Convert to the application layer presentation and return
                return new WithdrawLimitRepresentation(accountWithdrawLimits.Currency.Name, 
                    accountWithdrawLimits.AccountId.Value, accountWithdrawLimits.DailyLimit,
                    accountWithdrawLimits.DailyLimitUsed, accountWithdrawLimits.MonthlyLimit, 
                    accountWithdrawLimits.MonthlyLimitUsed, accountWithdrawLimits.CurrentBalance, 
                    accountWithdrawLimits.MaximumWithdraw, accountWithdrawLimits.MaximumWithdrawInUsd,
                    accountWithdrawLimits.Withheld, accountWithdrawLimits.WithheldInUsd, accountWithdrawLimits.Fee,
                    accountWithdrawLimits.MinimumAmount);
            }
            return null;
        }

        /// <summary>
        /// Deletes the given address from the database
        /// </summary>
        /// <param name="deleteWithdrawAddressCommand"></param>
        /// <returns></returns>
        public DeleteWithdrawAddressResponse DeleteAddress(DeleteWithdrawAddressCommand deleteWithdrawAddressCommand)
        {
            List<WithdrawAddress> withdrawAddresses = _withdrawAddressRepository.GetWithdrawAddressByAccountId(new AccountId(deleteWithdrawAddressCommand.AccountId));
            foreach (var withdrawAddress in withdrawAddresses)
            {
                if (withdrawAddress.BitcoinAddress.Value == deleteWithdrawAddressCommand.BitcoinAddress)
                {
                    _fundsPersistenceRepository.Delete(withdrawAddress);
                    return new DeleteWithdrawAddressResponse(true, "Deletion successful");
                }
            }
            return new DeleteWithdrawAddressResponse(false, "Address not found");
        }

        /// <summary>
        /// Cancel the withdraw with the given WithdrawID
        /// </summary>
        /// <param name="cancelWithdrawCommand"> </param>
        /// <returns></returns>
        public CancelWithdrawResponse CancelWithdraw(CancelWithdrawCommand cancelWithdrawCommand)
        {
            if (_withdrawSubmissionService.CancelWithdraw(cancelWithdrawCommand.WithdrawId))
            {
                Withdraw withdraw = _withdrawRepository.GetWithdrawByWithdrawId(cancelWithdrawCommand.WithdrawId);
                if (withdraw != null)
                {
                    withdraw.StatusCancelled();
                    _fundsPersistenceRepository.SaveOrUpdate(withdraw);
                    Balance balance = _balanceRepository.GetBalanceByCurrencyAndAccountId(withdraw.Currency, withdraw.AccountId);
                    if (balance != null)
                    {
                        if(balance.CancelPendingTransaction(withdraw.WithdrawId, PendingTransactionType.Withdraw,
                                                         withdraw.Amount + withdraw.Fee))
                        {
                            _fundsPersistenceRepository.SaveOrUpdate(balance);
                            return new CancelWithdrawResponse(true);
                        }
                    }
                    throw new NullReferenceException(string.Format("No balance found for Currency = {0} | Account ID = {1}",
                        withdraw.Currency.Name, withdraw.AccountId.Value));
                }
                throw new NullReferenceException(string.Format("No withdraw found in the repository for Withdraw ID: {0}",
                cancelWithdrawCommand.WithdrawId)); 
            }
            throw new ApplicationException(string.Format("Could not cancel withdraw with Withdraw ID: {0}", cancelWithdrawCommand.WithdrawId));
        }

        /// <summary>
        /// Get the WIthdraw Tier Limits
        /// </summary>
        /// <returns></returns>
        public WithdrawTierLimitRepresentation GetWithdrawTierLimits()
        {
            decimal tier0DailyLimit = 0;
            decimal tier0MonthlyLimit = 0;

            decimal tier1DailyLimit = 0;
            decimal tier1MonthlyLimit = 0;

            decimal tier2DailyLimit = 0;
            decimal tier2MonthlyLimit = 0;

            decimal tier3DailyLimit = 0;
            decimal tier3MonthlyLimit = 0;

            decimal tier4DailyLimit = 0;
            decimal tier4MonthlyLimit = 0;

            IList<WithdrawLimit> allWithdrawLimits = _withdrawLimitRepository.GetAllWithdrawLimits();
            if (allWithdrawLimits != null && allWithdrawLimits.Any())
            {
                foreach (WithdrawLimit withdrawLimit in allWithdrawLimits)
                {
                    if (withdrawLimit.TierLevel.Equals("Tier 0"))
                    {
                        tier0DailyLimit = withdrawLimit.DailyLimit;
                        tier0MonthlyLimit = withdrawLimit.MonthlyLimit;
                    }
                    if (withdrawLimit.TierLevel.Equals("Tier 1"))
                    {
                        tier1DailyLimit = withdrawLimit.DailyLimit;
                        tier1MonthlyLimit = withdrawLimit.MonthlyLimit;
                    }
                    if (withdrawLimit.TierLevel.Equals("Tier 2"))
                    {
                        tier2DailyLimit = withdrawLimit.DailyLimit;
                        tier2MonthlyLimit = withdrawLimit.MonthlyLimit;
                    }
                    if (withdrawLimit.TierLevel.Equals("Tier 3"))
                    {
                        tier3DailyLimit = withdrawLimit.DailyLimit;
                        tier3MonthlyLimit = withdrawLimit.MonthlyLimit;
                    }
                    if (withdrawLimit.TierLevel.Equals("Tier 4"))
                    {
                        tier4DailyLimit = withdrawLimit.DailyLimit;
                        tier4MonthlyLimit = withdrawLimit.MonthlyLimit;
                    }
                }
                return new WithdrawTierLimitRepresentation(
                    tier0DailyLimit, tier0MonthlyLimit, tier1DailyLimit, tier1MonthlyLimit, tier2DailyLimit, tier2MonthlyLimit,
                    tier3DailyLimit, tier3MonthlyLimit, tier4DailyLimit, tier4MonthlyLimit);

            }
            return null;
        }
    }
}
