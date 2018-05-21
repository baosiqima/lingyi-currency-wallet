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
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoinExchange.Common.Tests;
using CoinExchange.Common.Utility;
using CoinExchange.Funds.Domain.Model.BalanceAggregate;
using CoinExchange.Funds.Domain.Model.CurrencyAggregate;
using CoinExchange.Funds.Domain.Model.DepositAggregate;
using CoinExchange.Funds.Domain.Model.FeeAggregate;
using CoinExchange.Funds.Domain.Model.LedgerAggregate;
using CoinExchange.Funds.Domain.Model.Repositories;
using CoinExchange.Funds.Domain.Model.Services;
using CoinExchange.Funds.Domain.Model.WithdrawAggregate;
using NUnit.Framework;
using Spring.Context.Support;

namespace CoinExchange.Funds.Application.IntegrationTests
{
    [TestFixture]
    class EndToEndScenarioTests
    {
        private DatabaseUtility _databaseUtility;

        [SetUp]
        public void Setup()
        {
            var connection = ConfigurationManager.ConnectionStrings["MySql"].ToString();
            _databaseUtility = new DatabaseUtility(connection);
            _databaseUtility.Create();
            _databaseUtility.Populate();
        }

        [TearDown]
        public void Teardown()
        {
            _databaseUtility.Create();
        }

        #region Complete Mixed Scenario Tests

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Scenario1Test_TestsFundsValidationServiceOperationsInaRandomOrderToProceedInTheDesiredExpectenacy_VerifiesThroughDatabaseQuery()
        {
            // Deposit --> Order Validations --> Trade --> Withdraw
            IFundsValidationService fundsValidationService = (IFundsValidationService)ContextRegistry.GetContext()["FundsValidationService"];
            IFundsPersistenceRepository fundsPersistenceRepository = (IFundsPersistenceRepository)ContextRegistry.GetContext()["FundsPersistenceRepository"];
            IBalanceRepository balanceRepository = (IBalanceRepository)ContextRegistry.GetContext()["BalanceRepository"];
            IDepositIdGeneratorService depositIdGeneratorService = (IDepositIdGeneratorService)ContextRegistry.GetContext()["DepositIdGeneratorService"];
            IWithdrawFeesRepository withdrawFeesRepository = (IWithdrawFeesRepository)ContextRegistry.GetContext()["WithdrawFeesRepository"];
            IFeeCalculationService feeCalculationService = (IFeeCalculationService)ContextRegistry.GetContext()["FeeCalculationService"];
            IFeeRepository feeRepository = (IFeeRepository)ContextRegistry.GetContext()["FeeRepository"];

            Tuple<string, string> splittedCurrencyPair = CurrencySplitterService.SplitCurrencyPair(feeRepository.GetAllFees().First().CurrencyPair);
            Currency baseCurrency = new Currency(splittedCurrencyPair.Item1);
            Currency quoteCurrency = new Currency(splittedCurrencyPair.Item2);
                      
            AccountId user1Account = new AccountId(1);
            AccountId user2Account = new AccountId(2);
            decimal baseDepositAmount = 1.4m;
            decimal quoteDepositAmount = 1000;

            Balance user1QuoteInitialBalance = new Balance(quoteCurrency, user1Account, quoteDepositAmount, quoteDepositAmount);
            Balance user2QuoteInitialBalance = new Balance(quoteCurrency, user2Account, quoteDepositAmount, quoteDepositAmount);
            fundsPersistenceRepository.SaveOrUpdate(user1QuoteInitialBalance);
            fundsPersistenceRepository.SaveOrUpdate(user2QuoteInitialBalance);

            // Deposit
            Deposit deposit1 = new Deposit(baseCurrency, depositIdGeneratorService.GenerateId(), DateTime.Now,
                                          DepositType.Default, baseDepositAmount, 0, TransactionStatus.Pending, user1Account,
                                          new TransactionId("1"), new BitcoinAddress("bitcoin1"));
            deposit1.IncrementConfirmations(7);
            fundsPersistenceRepository.SaveOrUpdate(deposit1);

            Deposit deposit2 = new Deposit(baseCurrency, depositIdGeneratorService.GenerateId(), DateTime.Now,
                                          DepositType.Default, baseDepositAmount, 0, TransactionStatus.Pending, user2Account,
                                          new TransactionId("3"), new BitcoinAddress("bitcoin3"));
            deposit2.IncrementConfirmations(7);
            fundsPersistenceRepository.SaveOrUpdate(deposit2);

            // Retrieve Base Currency balance for user 1
            bool depositResponse = fundsValidationService.DepositConfirmed(deposit1);
            Assert.IsTrue(depositResponse);
            Balance balance = balanceRepository.GetBalanceByCurrencyAndAccountId(baseCurrency, user1Account);
            Assert.IsNotNull(balance);
            Assert.AreEqual(deposit1.Amount, balance.CurrentBalance);
            Assert.AreEqual(deposit1.Amount, balance.AvailableBalance);
            Assert.AreEqual(balance.PendingBalance, 0);

            // Retrieve Quote Currency balance for user 1
            balance = balanceRepository.GetBalanceByCurrencyAndAccountId(quoteCurrency, user1Account);
            Assert.IsNotNull(balance);
            Assert.AreEqual(user1QuoteInitialBalance.CurrentBalance, balance.CurrentBalance);
            Assert.AreEqual(user1QuoteInitialBalance.AvailableBalance, balance.AvailableBalance);
            Assert.AreEqual(0, balance.PendingBalance);

            // Retrieve BTC balance for user 2
            depositResponse = fundsValidationService.DepositConfirmed(deposit2);
            Assert.IsTrue(depositResponse);
            balance = balanceRepository.GetBalanceByCurrencyAndAccountId(baseCurrency, user2Account);
            Assert.IsNotNull(balance);
            Assert.AreEqual(deposit2.Amount, balance.CurrentBalance);
            Assert.AreEqual(deposit2.Amount, balance.AvailableBalance);
            Assert.AreEqual(balance.PendingBalance, 0);

            // Retrieve USD balance for user 2
            balance = balanceRepository.GetBalanceByCurrencyAndAccountId(quoteCurrency, user2Account);
            Assert.IsNotNull(balance);
            Assert.AreEqual(user2QuoteInitialBalance.CurrentBalance, balance.CurrentBalance);
            Assert.AreEqual(user2QuoteInitialBalance.AvailableBalance, balance.AvailableBalance);
            Assert.AreEqual(0, balance.PendingBalance);

            // Order Validation for User 1's Account
            decimal volume = 1.2m;
            decimal price = 590;
            string buy = "buy";
            string sell = "sell";
            string buyOrderId = "buy123";
            string sellOrderId = "sell123";
            bool validationResponse = fundsValidationService.ValidateFundsForOrder(user1Account, baseCurrency, quoteCurrency,
                volume, price, buy, buyOrderId);
            Assert.IsTrue(validationResponse);

            decimal user1Fee = feeCalculationService.GetFee(baseCurrency, quoteCurrency, user1Account, volume, price);
            decimal user2Fee = feeCalculationService.GetFee(baseCurrency, quoteCurrency, user2Account, volume, price);

            // Base Currency Balance for User Account 1
            balance = balanceRepository.GetBalanceByCurrencyAndAccountId(baseCurrency, user1Account);
            Assert.IsNotNull(balance);
            Assert.AreEqual(baseDepositAmount, balance.AvailableBalance);
            Assert.AreEqual(baseDepositAmount, balance.CurrentBalance);
            Assert.AreEqual(0, balance.PendingBalance);

            // Quote Currency Balance for User Account 1
            balance = balanceRepository.GetBalanceByCurrencyAndAccountId(quoteCurrency, user1Account);
            Assert.IsNotNull(balance);
            Assert.AreEqual(quoteDepositAmount - (volume * price), balance.AvailableBalance);
            Assert.AreEqual(quoteDepositAmount, balance.CurrentBalance);
            Assert.AreEqual(volume * price, balance.PendingBalance);

            // Validation of User 2's order
            validationResponse = fundsValidationService.ValidateFundsForOrder(user2Account, baseCurrency, quoteCurrency,
                volume, price, sell, sellOrderId);
            Assert.IsTrue(validationResponse);

            // Base Currency Balance for User Account 2
            balance = balanceRepository.GetBalanceByCurrencyAndAccountId(baseCurrency, user2Account);
            Assert.IsNotNull(balance);
            Assert.AreEqual(baseDepositAmount - volume, balance.AvailableBalance);
            Assert.AreEqual(baseDepositAmount, balance.CurrentBalance);
            Assert.AreEqual(volume, balance.PendingBalance);

            // Quote Currency Balance for User Account 2
            balance = balanceRepository.GetBalanceByCurrencyAndAccountId(quoteCurrency, user2Account);
            Assert.IsNotNull(balance);
            Assert.AreEqual(quoteDepositAmount, balance.AvailableBalance);
            Assert.AreEqual(quoteDepositAmount, balance.CurrentBalance);
            Assert.AreEqual(0, balance.PendingBalance);

            string tradeId = "tradeid123";
            bool tradeExecutedResponse = fundsValidationService.TradeExecuted(baseCurrency.Name, quoteCurrency.Name,
                volume, price, DateTime.Now, tradeId, user1Account.Value, user2Account.Value, buyOrderId, sellOrderId);
            Assert.IsTrue(tradeExecutedResponse);

            // Base Currency Balance for User Account 1
            balance = balanceRepository.GetBalanceByCurrencyAndAccountId(baseCurrency, user1Account);
            Assert.IsNotNull(balance);
            Assert.AreEqual(baseDepositAmount + volume, balance.AvailableBalance);
            Assert.AreEqual(baseDepositAmount + volume, balance.CurrentBalance);
            Assert.AreEqual(0, balance.PendingBalance);

            // Quote Currency Balance for User Account 1
            balance = balanceRepository.GetBalanceByCurrencyAndAccountId(quoteCurrency, user1Account);
            Assert.IsNotNull(balance);
            Assert.AreEqual(quoteDepositAmount - (volume * price) - user1Fee, balance.AvailableBalance);
            Assert.AreEqual(quoteDepositAmount - (volume * price) - user1Fee, balance.CurrentBalance);
            Assert.AreEqual(0, balance.PendingBalance);

            // Base Currency Balance for User Account 2
            balance = balanceRepository.GetBalanceByCurrencyAndAccountId(baseCurrency, user2Account);
            Assert.IsNotNull(balance);
            Assert.AreEqual(baseDepositAmount - (volume), balance.AvailableBalance);
            Assert.AreEqual(baseDepositAmount - (volume), balance.CurrentBalance);
            Assert.AreEqual(0, balance.PendingBalance);

            // Quote Currency Balance for User Account 2
            balance = balanceRepository.GetBalanceByCurrencyAndAccountId(quoteCurrency, user2Account);
            Assert.IsNotNull(balance);
            Assert.AreEqual(quoteDepositAmount + (volume * price) - user2Fee, balance.AvailableBalance);
            Assert.AreEqual(quoteDepositAmount + (volume * price) - user2Fee, balance.CurrentBalance);
            Assert.AreEqual(0, balance.PendingBalance);

            decimal withdrawAmount = 0.3M;
            // Withdraw Base Currency
            Withdraw validateFundsForWithdrawal = fundsValidationService.ValidateFundsForWithdrawal(user1Account, 
                baseCurrency, withdrawAmount, new TransactionId("transaction123"), new BitcoinAddress("bitcoin123"));
            Assert.IsNotNull(validateFundsForWithdrawal);
            WithdrawFees withdrawFee = withdrawFeesRepository.GetWithdrawFeesByCurrencyName(baseCurrency.Name);

            // Base Currency Balance for User Account 1
            balance = balanceRepository.GetBalanceByCurrencyAndAccountId(baseCurrency, user1Account);
            Assert.IsNotNull(balance);
            Assert.AreEqual(baseDepositAmount + volume - withdrawAmount, balance.AvailableBalance);
            Assert.AreEqual(baseDepositAmount + volume, balance.CurrentBalance);
            Assert.AreEqual(withdrawAmount, balance.PendingBalance);

            bool withdrawalExecuted = fundsValidationService.WithdrawalExecuted(validateFundsForWithdrawal);
            Assert.IsTrue(withdrawalExecuted);

            // Base Currency Balance for User Account 1
            balance = balanceRepository.GetBalanceByCurrencyAndAccountId(baseCurrency, user1Account);
            Assert.IsNotNull(balance);
            Assert.AreEqual(baseDepositAmount + volume - withdrawAmount, balance.AvailableBalance);
            Assert.AreEqual(baseDepositAmount + volume - withdrawAmount, balance.CurrentBalance);
            Assert.AreEqual(0, balance.PendingBalance);

            // EXCEPTION OCCURS HERE when the user tries to withdraw more than their remaining limit
            // Withdraw will fail because the amount requested is greater than the maximum limit threshold
            validateFundsForWithdrawal = fundsValidationService.ValidateFundsForWithdrawal(user1Account, 
                baseCurrency, baseDepositAmount + volume/*Excluding previous withdrawal amount*/, 
                new TransactionId("transaction123"), new BitcoinAddress("bitcoin123"));
            Assert.IsNull(validateFundsForWithdrawal);

            // Base Currency for User Account 1
            balance = balanceRepository.GetBalanceByCurrencyAndAccountId(baseCurrency, user1Account);
            Assert.IsNotNull(balance);
            Assert.AreEqual(baseDepositAmount + volume - withdrawAmount, balance.AvailableBalance);
            Assert.AreEqual(baseDepositAmount + volume - withdrawAmount, balance.CurrentBalance);
            Assert.AreEqual(0, balance.PendingBalance);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void DepositAndWithdrawTest1_TestsByMakingDepositsAndWIthdrawalsRandomly_VerifiesThroughDatabasequeriesAndReturnValues()
        {
            // Scenario: Withdraw(fail due to insufficient balance) -->
            // Deposit(Confirm) --> Withdraw(Pending) --> Withdraw(Fail due to insufficient available and enough 
            // pending balance) --> Withdraw(Success) --> Deposit(Fail due to over the limit)
            IFundsValidationService fundsValidationService = (IFundsValidationService)ContextRegistry.GetContext()["FundsValidationService"];
            IFundsPersistenceRepository fundsPersistenceRepository = (IFundsPersistenceRepository)ContextRegistry.GetContext()["FundsPersistenceRepository"];
            IBalanceRepository balanceRepository = (IBalanceRepository)ContextRegistry.GetContext()["BalanceRepository"];
            IDepositIdGeneratorService depositIdGeneratorService = (IDepositIdGeneratorService)ContextRegistry.GetContext()["DepositIdGeneratorService"];
            IWithdrawFeesRepository withdrawFeesRepository = (IWithdrawFeesRepository)ContextRegistry.GetContext()["WithdrawFeesRepository"];

            AccountId accountId = new AccountId(123);
            Currency currency = new Currency("BTC", true);

            Deposit deposit = new Deposit(currency, depositIdGeneratorService.GenerateId(), DateTime.Now,
                                          DepositType.Default, 1.4m, 0, TransactionStatus.Pending, accountId,
                                          new TransactionId("123"), new BitcoinAddress("bitcoin123"));
            deposit.IncrementConfirmations(7);
            fundsPersistenceRepository.SaveOrUpdate(deposit);

            // Retrieve balance
            Balance balance = balanceRepository.GetBalanceByCurrencyAndAccountId(currency, accountId);
            Assert.IsNull(balance);
            bool depositResponse = fundsValidationService.DepositConfirmed(deposit);
            Assert.IsTrue(depositResponse);

            // Check balance
            balance = balanceRepository.GetBalanceByCurrencyAndAccountId(currency, accountId);
            Assert.IsNotNull(balance);
            Assert.AreEqual(deposit.Amount, balance.CurrentBalance);
            Assert.AreEqual(deposit.Amount, balance.AvailableBalance);
            Assert.AreEqual(balance.PendingBalance, 0);

            WithdrawFees withdrawFee = withdrawFeesRepository.GetWithdrawFeesByCurrencyName(currency.Name);

            // Withdraw
            Withdraw withdrawal = fundsValidationService.ValidateFundsForWithdrawal(accountId, currency,
                1.3M, new TransactionId("transaction123"), new BitcoinAddress("bitcoin123"));
            Assert.IsNotNull(withdrawal);
            Assert.AreEqual(TransactionStatus.Pending, withdrawal.Status);

            // Check balance
            balance = balanceRepository.GetBalanceByCurrencyAndAccountId(currency, accountId);
            Assert.IsNotNull(balance);
            Assert.AreEqual(Math.Round(1.4M - 1.3M, 5), Math.Round(balance.AvailableBalance, 5));
            Assert.AreEqual(Math.Round(1.4M, 5), Math.Round(balance.CurrentBalance, 5));
            Assert.AreEqual(1.3m, balance.PendingBalance);

            // Withdraw # 1 Confirmed
            bool withdrawalExecuted = fundsValidationService.WithdrawalExecuted(withdrawal);
            Assert.IsTrue(withdrawalExecuted);
            Assert.AreEqual(TransactionStatus.Confirmed, withdrawal.Status);
            
            // Check balance
            balance = balanceRepository.GetBalanceByCurrencyAndAccountId(currency, accountId);
            Assert.IsNotNull(balance);
            Assert.AreEqual(Math.Round(1.4M - 1.3M, 5), Math.Round(balance.CurrentBalance, 5));
            Assert.AreEqual(Math.Round(1.4M - 1.3M, 5), Math.Round(balance.AvailableBalance, 5));
            Assert.AreEqual(0, balance.PendingBalance);

            // Withdraw # 2: Exception Expected
            withdrawal = fundsValidationService.ValidateFundsForWithdrawal(accountId, currency,
                0.7M, new TransactionId("transaction123"), new BitcoinAddress("bitcoin123"));
            Assert.IsNull(withdrawal);

            // Check balance
            balance = balanceRepository.GetBalanceByCurrencyAndAccountId(currency, accountId);
            Assert.IsNotNull(balance);
            Assert.AreEqual(Math.Round(1.4M - 1.3M, 5), Math.Round(balance.CurrentBalance, 5));
            Assert.AreEqual(Math.Round(1.4M - 1.3M, 5), Math.Round(balance.AvailableBalance, 5));
            Assert.AreEqual(0, balance.PendingBalance);

            // Withdraw # 3
            withdrawal = fundsValidationService.ValidateFundsForWithdrawal(accountId, currency,
                0.098M, new TransactionId("transaction123"), new BitcoinAddress("bitcoin123"));
            Assert.IsNotNull(withdrawal);
            Assert.AreEqual(TransactionStatus.Pending, withdrawal.Status);
            balance = balanceRepository.GetBalanceByCurrencyAndAccountId(currency, accountId);
            Assert.IsNotNull(balance);
            Assert.AreEqual(Math.Round(1.4M - 1.3M - 0.1M, 5), Math.Round(balance.AvailableBalance, 5));
            Assert.AreEqual(Math.Round(1.4M - 1.3M, 5), Math.Round(balance.CurrentBalance, 5));
            Assert.AreEqual(0.1M, balance.PendingBalance);

            // Withdraw # 3 Confirmed
            withdrawalExecuted = fundsValidationService.WithdrawalExecuted(withdrawal);
            Assert.IsTrue(withdrawalExecuted);
            Assert.AreEqual(TransactionStatus.Confirmed, withdrawal.Status);

            // Check balance
            balance = balanceRepository.GetBalanceByCurrencyAndAccountId(currency, accountId);
            Assert.IsNotNull(balance);
            Assert.AreEqual(Math.Round(1.4M - 1.3M - 0.1M, 5), Math.Round(balance.CurrentBalance, 5));
            Assert.AreEqual(Math.Round(1.4M - 1.3M - 0.1M, 5), Math.Round(balance.AvailableBalance, 5));
            Assert.AreEqual(0, balance.PendingBalance);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void DepositAndWithdrawTest2_TestsByMakingDepositsAndWIthdrawalsRandomly_VerifiesThroughDatabasequeriesAndReturnValues()
        {
            // Scenario: Withdraw(fail due to insufficient balance) -->
            // Deposit(Confirm) --> Withdraw(Pending) --> Withdraw(Fail due to insufficient available and enough 
            // pending balance) --> Withdraw(Success) --> Deposit(Fail due to over the limit)
            IFundsValidationService fundsValidationService = (IFundsValidationService)ContextRegistry.GetContext()["FundsValidationService"];
            IFundsPersistenceRepository fundsPersistenceRepository = (IFundsPersistenceRepository)ContextRegistry.GetContext()["FundsPersistenceRepository"];
            IBalanceRepository balanceRepository = (IBalanceRepository)ContextRegistry.GetContext()["BalanceRepository"];
            IDepositIdGeneratorService depositIdGeneratorService = (IDepositIdGeneratorService)ContextRegistry.GetContext()["DepositIdGeneratorService"];
            IWithdrawFeesRepository withdrawFeesRepository = (IWithdrawFeesRepository)ContextRegistry.GetContext()["WithdrawFeesRepository"];

            AccountId accountId = new AccountId(123);
            Currency currency = new Currency("BTC", true);

            Deposit deposit = new Deposit(currency, depositIdGeneratorService.GenerateId(), DateTime.Now,
                                          DepositType.Default, 1.4m, 0, TransactionStatus.Pending, accountId,
                                          new TransactionId("123"), new BitcoinAddress("bitcoin123"));
            deposit.IncrementConfirmations(7);
            fundsPersistenceRepository.SaveOrUpdate(deposit);

            // Retrieve balance
            Balance balance = balanceRepository.GetBalanceByCurrencyAndAccountId(currency, accountId);
            Assert.IsNull(balance);

            // Deposit
            bool depositResponse = fundsValidationService.DepositConfirmed(deposit);
            Assert.IsTrue(depositResponse);

            // Check balance
            balance = balanceRepository.GetBalanceByCurrencyAndAccountId(currency, accountId);
            Assert.IsNotNull(balance);
            Assert.AreEqual(deposit.Amount, balance.CurrentBalance);
            Assert.AreEqual(deposit.Amount, balance.AvailableBalance);
            Assert.AreEqual(balance.PendingBalance, 0);

            WithdrawFees withdrawFee = withdrawFeesRepository.GetWithdrawFeesByCurrencyName(currency.Name);

            // Withdraw
            Withdraw withdrawal = fundsValidationService.ValidateFundsForWithdrawal(accountId, currency,
                1.3M, new TransactionId("transaction123"), new BitcoinAddress("bitcoin123"));
            Assert.IsNotNull(withdrawal);
            Assert.AreEqual(TransactionStatus.Pending, withdrawal.Status);

            // Check balance
            balance = balanceRepository.GetBalanceByCurrencyAndAccountId(currency, accountId);
            Assert.IsNotNull(balance);
            Assert.AreEqual(Math.Round(1.4M - 1.3M, 5), Math.Round(balance.AvailableBalance, 5));
            Assert.AreEqual(Math.Round(1.4M, 5), Math.Round(balance.CurrentBalance, 5));
            Assert.AreEqual(1.3m, balance.PendingBalance);

            // Withdraw # 1 Confirmed
            bool withdrawalExecuted = fundsValidationService.WithdrawalExecuted(withdrawal);
            Assert.IsTrue(withdrawalExecuted);
            Assert.AreEqual(TransactionStatus.Confirmed, withdrawal.Status);

            // Check balance
            balance = balanceRepository.GetBalanceByCurrencyAndAccountId(currency, accountId);
            Assert.IsNotNull(balance);
            Assert.AreEqual(Math.Round(1.4M - 1.3M, 5), Math.Round(balance.CurrentBalance, 5));
            Assert.AreEqual(Math.Round(1.4M - 1.3M, 5), Math.Round(balance.AvailableBalance, 5));
            Assert.AreEqual(0, balance.PendingBalance);

            // Withdraw # 2: Exception Expected
            withdrawal = fundsValidationService.ValidateFundsForWithdrawal(accountId, currency,
                0.7M, new TransactionId("transaction123"), new BitcoinAddress("bitcoin123"));
            Assert.IsNull(withdrawal);

            // Check balance
            balance = balanceRepository.GetBalanceByCurrencyAndAccountId(currency, accountId);
            Assert.IsNotNull(balance);
            Assert.AreEqual(Math.Round(1.4M - 1.3M, 5), Math.Round(balance.CurrentBalance, 5));
            Assert.AreEqual(Math.Round(1.4M - 1.3M, 5), Math.Round(balance.AvailableBalance, 5));
            Assert.AreEqual(0, balance.PendingBalance);

            // Withdraw # 3
            withdrawal = fundsValidationService.ValidateFundsForWithdrawal(accountId, currency,
                0.098M, new TransactionId("transaction123"), new BitcoinAddress("bitcoin123"));
            Assert.IsNotNull(withdrawal);
            Assert.AreEqual(TransactionStatus.Pending, withdrawal.Status);
            balance = balanceRepository.GetBalanceByCurrencyAndAccountId(currency, accountId);
            Assert.IsNotNull(balance);
            Assert.AreEqual(Math.Round(1.4M - 1.3M - 0.1M - (withdrawFee.Fee * 2), 5), Math.Round(balance.AvailableBalance, 5));
            Assert.AreEqual(Math.Round(1.4M - 1.3M, 5), Math.Round(balance.CurrentBalance, 5));
            Assert.AreEqual(0.1M, balance.PendingBalance);

            // Withdraw # 3 Confirmed
            withdrawalExecuted = fundsValidationService.WithdrawalExecuted(withdrawal);
            Assert.IsTrue(withdrawalExecuted);
            Assert.AreEqual(TransactionStatus.Confirmed, withdrawal.Status);

            // Check balance
            balance = balanceRepository.GetBalanceByCurrencyAndAccountId(currency, accountId);
            Assert.IsNotNull(balance);
            Assert.AreEqual(Math.Round(1.4M - 1.3M - 0.1M - (withdrawFee.Fee * 2), 5), Math.Round(balance.CurrentBalance, 5));
            Assert.AreEqual(Math.Round(1.4M - 1.3M - 0.1M - (withdrawFee.Fee * 2), 5), Math.Round(balance.AvailableBalance, 5));
            Assert.AreEqual(0, balance.PendingBalance);

            // Over the limit Deposit
            deposit = new Deposit(currency, depositIdGeneratorService.GenerateId(), DateTime.Now,
                                          DepositType.Default, 1.4m, 0, TransactionStatus.Pending, accountId,
                                          new TransactionId("123"), new BitcoinAddress("bitcoin123"));
            deposit.IncrementConfirmations(7);
            fundsPersistenceRepository.SaveOrUpdate(deposit);

            fundsValidationService.DepositConfirmed(deposit);
            // Check balance
            balance = balanceRepository.GetBalanceByCurrencyAndAccountId(currency, accountId);
            Assert.IsNotNull(balance);
            Assert.AreEqual(Math.Round(1.4M - 1.3M - 0.1M - (withdrawFee.Fee * 2), 5), Math.Round(balance.CurrentBalance, 5));
            Assert.AreEqual(Math.Round(1.4M - 1.3M - 0.1M - (withdrawFee.Fee * 2), 5), Math.Round(balance.AvailableBalance, 5));
            Assert.IsTrue(balance.IsFrozen);
        }

        #endregion Complete Mixed Scenario Tests

        #region Order Scenario Tests

        [Test]
        public void SendMultipleOrdersTest_TestsIfSendingMultipleOrdersIsHandledAsExpected_VerifiesThroughTheReturnesValue()
        {
            IFundsValidationService fundsValidationService = (IFundsValidationService)ContextRegistry.GetContext()["FundsValidationService"];
            IFundsPersistenceRepository fundsPersistenceRepository = (IFundsPersistenceRepository)ContextRegistry.GetContext()["FundsPersistenceRepository"];
            IBalanceRepository balanceRepository = (IBalanceRepository)ContextRegistry.GetContext()["BalanceRepository"];
            IFeeCalculationService feeCalculationService = (IFeeCalculationService)ContextRegistry.GetContext()["FeeCalculationService"];
            IFeeRepository feeRepository = (IFeeRepository)ContextRegistry.GetContext()["FeeRepository"];

            AccountId accountId = new AccountId(123);
            List<Fee> allFees = feeRepository.GetAllFees();
            Tuple<string, string> splittedCurrencyPair = CurrencySplitterService.SplitCurrencyPair(allFees.First().CurrencyPair);
            Currency baseCurrency = new Currency(splittedCurrencyPair.Item1);
            Currency quoteCurrency = new Currency(splittedCurrencyPair.Item2);
            Balance baseCurrencyBalance = new Balance(baseCurrency, accountId, 400, 400);
            fundsPersistenceRepository.SaveOrUpdate(baseCurrencyBalance);

            Balance quoteCurrencyBalance = new Balance(quoteCurrency, accountId, 40000, 40000);
            fundsPersistenceRepository.SaveOrUpdate(quoteCurrencyBalance);

            bool validateFundsForOrder = fundsValidationService.ValidateFundsForOrder(accountId, baseCurrency,
                quoteCurrency, 40, 100, "buy", "order123");
            Assert.IsTrue(validateFundsForOrder);

            // Get the fee corresponding to the current volume of the quote currency
            decimal usdFee = feeCalculationService.GetFee(baseCurrency, quoteCurrency, accountId, 40, 100);
            Assert.Greater(usdFee, 0);

            baseCurrencyBalance = balanceRepository.GetBalanceByCurrencyAndAccountId(baseCurrency, accountId);
            Assert.AreEqual(400, baseCurrencyBalance.CurrentBalance);
            Assert.AreEqual(400, baseCurrencyBalance.AvailableBalance);
            Assert.AreEqual(0, baseCurrencyBalance.PendingBalance);

            quoteCurrencyBalance = balanceRepository.GetBalanceByCurrencyAndAccountId(quoteCurrency, accountId);
            Assert.AreEqual(40000 - (40 * 100), quoteCurrencyBalance.AvailableBalance);
            Assert.AreEqual(40000, quoteCurrencyBalance.CurrentBalance);
            Assert.AreEqual(40 * 100, quoteCurrencyBalance.PendingBalance);            
        }

        #endregion Order Scenario Tests

        #region Deposit and Withdrawal Tests

        [Test]
        public void DepositAndWithdrawTest_TestsIfThingsGoAsExpectedWhenWithdrawIsMadeAfterDeposit_ChecksBalanceToVerify()
        {
            // Scenario: Confirmed Deposit --> Withdraw --> Check Balance            
            IFundsValidationService fundsValidationService = (IFundsValidationService)ContextRegistry.GetContext()["FundsValidationService"];
            IFundsPersistenceRepository fundsPersistenceRepository = (IFundsPersistenceRepository)ContextRegistry.GetContext()["FundsPersistenceRepository"];
            IBalanceRepository balanceRepository = (IBalanceRepository)ContextRegistry.GetContext()["BalanceRepository"];            
            IDepositIdGeneratorService depositIdGeneratorService = (IDepositIdGeneratorService)ContextRegistry.GetContext()["DepositIdGeneratorService"];
            IWithdrawFeesRepository withdrawFeesRepository = (IWithdrawFeesRepository)ContextRegistry.GetContext()["WithdrawFeesRepository"];
            
            AccountId accountId = new AccountId(123);
            Currency currency = new Currency("BTC", true);

            // Deposit
            Deposit deposit = new Deposit(currency, depositIdGeneratorService.GenerateId(), DateTime.Now,
                                          DepositType.Default, 1.4m, 0, TransactionStatus.Pending, accountId,
                                          new TransactionId("123"), new BitcoinAddress("bitcoin123"));
            deposit.IncrementConfirmations(7);
            fundsPersistenceRepository.SaveOrUpdate(deposit);

            // Retrieve balance
            Balance balance = balanceRepository.GetBalanceByCurrencyAndAccountId(currency, accountId);
            Assert.IsNull(balance);
            bool depositResponse = fundsValidationService.DepositConfirmed(deposit);
            Assert.IsTrue(depositResponse);

            // Check balance
            balance = balanceRepository.GetBalanceByCurrencyAndAccountId(currency, accountId);
            Assert.IsNotNull(balance);
            Assert.AreEqual(balance.CurrentBalance, deposit.Amount);
            Assert.AreEqual(balance.AvailableBalance, deposit.Amount);
            Assert.AreEqual(balance.PendingBalance, 0);

            // Withdraw
            Withdraw validateFundsForWithdrawal = fundsValidationService.ValidateFundsForWithdrawal(accountId, currency,
                1.3M, new TransactionId("transaction123"), new BitcoinAddress("bitcoin123"));
            Assert.IsNotNull(validateFundsForWithdrawal);
            bool withdrawalExecuted = fundsValidationService.WithdrawalExecuted(validateFundsForWithdrawal);
            Assert.IsTrue(withdrawalExecuted);

            WithdrawFees withdrawFee = withdrawFeesRepository.GetWithdrawFeesByCurrencyName(currency.Name);

            // Check balance
            balance = balanceRepository.GetBalanceByCurrencyAndAccountId(currency, accountId);
            Assert.IsNotNull(balance);
            Assert.AreEqual(Math.Round(1.4M - 1.3M, 5), Math.Round(balance.CurrentBalance, 5));
            Assert.AreEqual(Math.Round(1.4M - 1.3M, 5), Math.Round(balance.AvailableBalance, 5));
            Assert.AreEqual(0, balance.PendingBalance);
        }

        #endregion Deposit and Withdrawal Tests

        #region Orders And Trades

        [Test]
        public void ValidateOrderAndHandleTradeTest_CheksIfEnoughBalanceIsAvailableForAnOrderToBeSentAndHandlesTradeWhenItExectues_VerifiesThroughDatabaseQueryAndValueReturned()
        {
            IFundsValidationService fundsValidationService = (IFundsValidationService)ContextRegistry.GetContext()["FundsValidationService"];
            IFundsPersistenceRepository fundsPersistenceRepository = (IFundsPersistenceRepository)ContextRegistry.GetContext()["FundsPersistenceRepository"];
            IBalanceRepository balanceRepository = (IBalanceRepository)ContextRegistry.GetContext()["BalanceRepository"];
            IDepositIdGeneratorService depositIdGeneratorService = (IDepositIdGeneratorService)ContextRegistry.GetContext()["DepositIdGeneratorService"];
            IWithdrawFeesRepository withdrawFeesRepository = (IWithdrawFeesRepository)ContextRegistry.GetContext()["WithdrawFeesRepository"];
            IFeeCalculationService feeCalculationService = (IFeeCalculationService)ContextRegistry.GetContext()["FeeCalculationService"];

            Currency baseCurrency = new Currency("BTC");
            Currency quoteCurrency = new Currency("USD");
            decimal volume = 1.4m;
            decimal price = 590m;
            
            string buyOrderId = "buy123";
            string sellOrderId = "sell123";
            string buy = "buy";
            string sell = "sell";
            AccountId buyAccountId = new AccountId(1);
            AccountId sellAccountId = new AccountId(2);
            Balance buyBaseBalance = new Balance(baseCurrency, buyAccountId, 20, 20);
            Balance buyQuoteBalance = new Balance(quoteCurrency, buyAccountId, 15000, 15000);
            Balance sellBaseBalance = new Balance(baseCurrency, sellAccountId, 20, 20);
            Balance sellQuoteBalance = new Balance(quoteCurrency, sellAccountId, 15000, 15000);

            fundsPersistenceRepository.SaveOrUpdate(buyBaseBalance);
            fundsPersistenceRepository.SaveOrUpdate(buyQuoteBalance);
            fundsPersistenceRepository.SaveOrUpdate(sellBaseBalance);
            fundsPersistenceRepository.SaveOrUpdate(sellQuoteBalance);

            bool validationResponse = fundsValidationService.ValidateFundsForOrder(buyAccountId, baseCurrency, quoteCurrency,
                volume, price, buy, buyOrderId);
            Assert.IsTrue(validationResponse);

            validationResponse = fundsValidationService.ValidateFundsForOrder(sellAccountId, baseCurrency, quoteCurrency,
                volume, price, sell, sellOrderId);
            Assert.IsTrue(validationResponse);

            buyBaseBalance = balanceRepository.GetBalanceByCurrencyAndAccountId(baseCurrency, buyAccountId);
            Assert.AreEqual(20, buyBaseBalance.AvailableBalance);
            Assert.AreEqual(20, buyBaseBalance.CurrentBalance);
            Assert.AreEqual(0, buyBaseBalance.PendingBalance);

            buyQuoteBalance = balanceRepository.GetBalanceByCurrencyAndAccountId(quoteCurrency, buyAccountId);
            Assert.AreEqual(15000 - (price * volume), buyQuoteBalance.AvailableBalance);
            Assert.AreEqual(15000, buyQuoteBalance.CurrentBalance);
            Assert.AreEqual(price * volume, buyQuoteBalance.PendingBalance);

            sellBaseBalance = balanceRepository.GetBalanceByCurrencyAndAccountId(baseCurrency, sellAccountId);
            Assert.AreEqual(20 - volume, sellBaseBalance.AvailableBalance);
            Assert.AreEqual(20, sellBaseBalance.CurrentBalance);
            Assert.AreEqual(volume, sellBaseBalance.PendingBalance);

            sellQuoteBalance = balanceRepository.GetBalanceByCurrencyAndAccountId(quoteCurrency, sellAccountId);
            Assert.AreEqual(15000, sellQuoteBalance.AvailableBalance);
            Assert.AreEqual(15000, sellQuoteBalance.CurrentBalance);
            Assert.AreEqual(0, sellQuoteBalance.PendingBalance);

            string tradeId = "tradeid123";
            bool tradeExecutedResponse = fundsValidationService.TradeExecuted(baseCurrency.Name, quoteCurrency.Name,
                volume, price, DateTime.Now, tradeId, buyAccountId.Value, sellAccountId.Value, buyOrderId, sellOrderId);
            Assert.IsTrue(tradeExecutedResponse);

            decimal buySideFee = feeCalculationService.GetFee(baseCurrency, quoteCurrency, buyAccountId, volume, price);
            decimal sellSidefee = feeCalculationService.GetFee(baseCurrency, quoteCurrency, sellAccountId, volume, price);
            buyBaseBalance = balanceRepository.GetBalanceByCurrencyAndAccountId(baseCurrency, buyAccountId);
            Assert.AreEqual(20 + volume, buyBaseBalance.AvailableBalance);
            Assert.AreEqual(20 + volume, buyBaseBalance.CurrentBalance);
            Assert.AreEqual(0, buyBaseBalance.PendingBalance);

            buyQuoteBalance = balanceRepository.GetBalanceByCurrencyAndAccountId(quoteCurrency, buyAccountId);
            Assert.AreEqual(15000 - (price * volume) - buySideFee, buyQuoteBalance.AvailableBalance);
            Assert.AreEqual(15000 - (price * volume) - buySideFee, buyQuoteBalance.CurrentBalance);
            Assert.AreEqual(0, buyQuoteBalance.PendingBalance);

            sellBaseBalance = balanceRepository.GetBalanceByCurrencyAndAccountId(baseCurrency, sellAccountId);
            Assert.AreEqual(20 - volume, sellBaseBalance.AvailableBalance);
            Assert.AreEqual(20 - volume, sellBaseBalance.CurrentBalance);
            Assert.AreEqual(0, sellBaseBalance.PendingBalance);

            sellQuoteBalance = balanceRepository.GetBalanceByCurrencyAndAccountId(quoteCurrency, sellAccountId);
            Assert.AreEqual(15000 + (price * volume) - sellSidefee, sellQuoteBalance.AvailableBalance);
            Assert.AreEqual(15000 + (price * volume) - sellSidefee, sellQuoteBalance.CurrentBalance);
            Assert.AreEqual(0, sellQuoteBalance.PendingBalance);
        }

        #endregion Orders And Trades

        #region Private Methods

        private decimal ConvertCurrencyToUsd(decimal bestBid, decimal bestAsk, decimal currencyAmount)
        {
            decimal sum = (currencyAmount * bestBid) + (currencyAmount * bestAsk);
            decimal midPoint = sum / 2;
            return midPoint;
        }

        #endregion Private Methods
    }
}
