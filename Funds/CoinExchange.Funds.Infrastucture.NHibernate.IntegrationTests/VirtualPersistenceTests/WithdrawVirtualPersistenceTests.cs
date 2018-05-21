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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CoinExchange.Funds.Domain.Model.CurrencyAggregate;
using CoinExchange.Funds.Domain.Model.DepositAggregate;
using CoinExchange.Funds.Domain.Model.Repositories;
using CoinExchange.Funds.Domain.Model.WithdrawAggregate;
using NUnit.Framework;

namespace CoinExchange.Funds.Infrastucture.NHibernate.IntegrationTests.VirtualPersistenceTests
{
    /// <summary>
    /// Tests that do not actually save the objects in the database, but use the configuration for NHibernate to virtually 
    /// save and retreive objects on the fly
    /// </summary>
    [TestFixture]
    class WithdrawVirtualPersistenceTests : AbstractConfiguration
    {
        private IFundsPersistenceRepository _persistanceRepository;
        private IWithdrawRepository _withdrawRepository;

        /// <summary>
        /// Spring's Injection using FundsAbstractConfiguration class
        /// </summary>
        public IWithdrawRepository WithdrawRepository
        {
            set { _withdrawRepository = value; }
        }

        /// <summary>
        /// Spring's Injection using FundsAbstractConfiguration class
        /// </summary>
        public IFundsPersistenceRepository Persistance
        {
            set { _persistanceRepository = value; }
        }

        [Test]
        public void SaveWithdrawalAndRetreiveByIdTest_SavesAnObjectToDatabaseAndManipulatesIt_ChecksIfItIsUpdatedAsExpected()
        {
            Withdraw withdraw = new Withdraw(new Currency("LTC", true), "1234", DateTime.Now, WithdrawType.Bitcoin, 2000,
                0.005m, TransactionStatus.Pending,
                new AccountId(1), new BitcoinAddress("bitcoin123"));

            _persistanceRepository.SaveOrUpdate(withdraw);

            Withdraw retrievedDeposit = _withdrawRepository.GetWithdrawByWithdrawId("1234");
            Assert.IsNotNull(retrievedDeposit);
            int id = retrievedDeposit.Id;
            retrievedDeposit.SetAmount(777);
            _persistanceRepository.SaveOrUpdate(retrievedDeposit);

            retrievedDeposit = _withdrawRepository.GetWithdrawById(id);
            Assert.AreEqual(withdraw.Currency.Name, retrievedDeposit.Currency.Name);
            Assert.AreEqual(withdraw.WithdrawId, retrievedDeposit.WithdrawId);
            Assert.AreEqual(withdraw.Type, retrievedDeposit.Type);
            Assert.AreEqual(777, retrievedDeposit.Amount);
            Assert.AreEqual(withdraw.Fee, retrievedDeposit.Fee);
            Assert.AreEqual(withdraw.Status, retrievedDeposit.Status);
            Assert.AreEqual(withdraw.AccountId.Value, retrievedDeposit.AccountId.Value);
        }

        [Test]
        public void SaveWithdrawalAndRetreiveByWithdrawIdTest_SavesAnObjectToDatabaseAndManipulatesIt_ChecksIfItIsUpdatedAsExpected()
        {
            Withdraw withdraw = new Withdraw(new Currency("LTC", true), "1234", DateTime.Now, WithdrawType.Bitcoin, 2000, 0.005m, TransactionStatus.Pending,
                new AccountId(1), new BitcoinAddress("bitcoin123"));

            _persistanceRepository.SaveOrUpdate(withdraw);

            Withdraw retrievedDeposit = _withdrawRepository.GetWithdrawByWithdrawId("1234");
            Assert.IsNotNull(retrievedDeposit);
            retrievedDeposit.SetAmount(777);
            _persistanceRepository.SaveOrUpdate(retrievedDeposit);

            retrievedDeposit = _withdrawRepository.GetWithdrawByWithdrawId("1234");
            Assert.AreEqual(withdraw.Currency.Name, retrievedDeposit.Currency.Name);
            Assert.AreEqual(withdraw.WithdrawId, retrievedDeposit.WithdrawId);
            Assert.AreEqual(withdraw.Type, retrievedDeposit.Type);
            Assert.AreEqual(777, retrievedDeposit.Amount);
            Assert.AreEqual(withdraw.Fee, retrievedDeposit.Fee);
            Assert.AreEqual(withdraw.Status, retrievedDeposit.Status);
            Assert.AreEqual(withdraw.AccountId.Value, retrievedDeposit.AccountId.Value);
        }

        [Test]
        public void SaveWithdrawalAndRetreiveByCurrencyNameTest_SavesAnObjectToDatabaseAndManipulatesIt_ChecksIfItIsUpdatedAsExpected()
        {
            Withdraw withdraw = new Withdraw(new Currency("LTC", true), "1234", DateTime.Now, WithdrawType.Bitcoin, 2000, 0.005m, TransactionStatus.Pending,
                new AccountId(1), new BitcoinAddress("bitcoin123"));

            _persistanceRepository.SaveOrUpdate(withdraw);

            List<Withdraw> retrievedDeposits = _withdrawRepository.GetWithdrawByCurrencyName("LTC");
            Assert.IsNotNull(retrievedDeposits);
            retrievedDeposits[0].SetAmount(777);
            _persistanceRepository.SaveOrUpdate(retrievedDeposits[0]);

            retrievedDeposits = _withdrawRepository.GetWithdrawByCurrencyName("LTC");
            Assert.AreEqual(withdraw.Currency.Name, retrievedDeposits[0].Currency.Name);
            Assert.AreEqual(withdraw.WithdrawId, retrievedDeposits[0].WithdrawId);
            Assert.AreEqual(withdraw.Type, retrievedDeposits[0].Type);
            Assert.AreEqual(777, retrievedDeposits[0].Amount);
            Assert.AreEqual(withdraw.Fee, retrievedDeposits[0].Fee);
            Assert.AreEqual(withdraw.Status, retrievedDeposits[0].Status);
            Assert.AreEqual(withdraw.AccountId.Value, retrievedDeposits[0].AccountId.Value);
        }

        [Test]
        public void SaveWithdrawalsAndRetreiveByAccountIdTest_SavesMultipleObjectInDatabase_ChecksIfTheoutputIsAsExpected()
        {
            Withdraw withdraw = new Withdraw(new Currency("LTC", true), "1234", DateTime.Now, WithdrawType.Bitcoin, 2000, 0.005m, TransactionStatus.Pending,
                new AccountId(1), new BitcoinAddress("bitcoin123"));

            _persistanceRepository.SaveOrUpdate(withdraw);

            Withdraw withdraw2 = new Withdraw(new Currency("BTC", true), "123", DateTime.Now, WithdrawType.Bitcoin, 1000, 0.010m, TransactionStatus.Pending,
                new AccountId(1), new BitcoinAddress("bitcoin123"));
            Thread.Sleep(500);

            _persistanceRepository.SaveOrUpdate(withdraw2);

            List<Withdraw> retrievedDepositList = _withdrawRepository.GetWithdrawByAccountId(new AccountId(1));
            Assert.IsNotNull(retrievedDepositList);
            Assert.AreEqual(2, retrievedDepositList.Count);

            Assert.AreEqual(withdraw.Currency.Name, retrievedDepositList[0].Currency.Name);
            Assert.AreEqual(withdraw.WithdrawId, retrievedDepositList[0].WithdrawId);
            Assert.AreEqual(withdraw.Type, retrievedDepositList[0].Type);
            Assert.AreEqual(withdraw.Amount, retrievedDepositList[0].Amount);
            Assert.AreEqual(withdraw.Fee, retrievedDepositList[0].Fee);
            Assert.AreEqual(withdraw.Status, retrievedDepositList[0].Status);
            Assert.AreEqual(withdraw.AccountId.Value, retrievedDepositList[0].AccountId.Value);

            Assert.AreEqual(withdraw2.Currency.Name, retrievedDepositList[1].Currency.Name);
            Assert.AreEqual(withdraw2.WithdrawId, retrievedDepositList[1].WithdrawId);
            Assert.AreEqual(withdraw2.Type, retrievedDepositList[1].Type);
            Assert.AreEqual(withdraw2.Amount, retrievedDepositList[1].Amount);
            Assert.AreEqual(withdraw2.Fee, retrievedDepositList[1].Fee);
            Assert.AreEqual(withdraw2.Status, retrievedDepositList[1].Status);
            Assert.AreEqual(withdraw2.AccountId.Value, retrievedDepositList[1].AccountId.Value);
        }
    }
}
