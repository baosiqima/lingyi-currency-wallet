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
using System.Threading;
using CoinExchange.Common.Tests;
using CoinExchange.Funds.Domain.Model.DepositAggregate;
using CoinExchange.Funds.Domain.Model.Repositories;
using CoinExchange.Funds.Domain.Model.CurrencyAggregate;
using NUnit.Framework;
using Spring.Context.Support;

namespace CoinExchange.Funds.Infrastucture.NHibernate.IntegrationTests.DatabasePersistenceTests
{
    /// <summary>
    /// Tests for actual persistence in database for Deposit address objects
    /// </summary>
    [TestFixture]
    class DepositAddressDatabasePersistenceTests
    {
        private DatabaseUtility _databaseUtility;
        private IFundsPersistenceRepository _persistanceRepository;
        private IDepositAddressRepository _depositAddressRepository;

        [SetUp]
        public void Setup()
        {
            _depositAddressRepository = (IDepositAddressRepository)ContextRegistry.GetContext()["DepositAddressRepository"];
            _persistanceRepository = (IFundsPersistenceRepository)ContextRegistry.GetContext()["FundsPersistenceRepository"];

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

        [Test]
        public void SaveDepositAddressesAndRetreiveByAccountIdTest_SavesObjectsToDatabase_ChecksIfTheyAreAsExpected()
        {
            DepositAddress deposit = new DepositAddress(new Currency("XBT", true), new BitcoinAddress("iambitcoin123"), AddressStatus.New, DateTime.Now,
                new AccountId(1));

            _persistanceRepository.SaveOrUpdate(deposit);

            DepositAddress deposit2 = new DepositAddress(new Currency("XBT", true), new BitcoinAddress("iambitcoin123"), AddressStatus.New, DateTime.Now,
                new AccountId(1));
            Thread.Sleep(500);

            _persistanceRepository.SaveOrUpdate(deposit2);

            List<DepositAddress> retrievedDepositAddressList = _depositAddressRepository.GetDepositAddressByAccountId(new AccountId(1));
            Assert.IsNotNull(retrievedDepositAddressList);
            Assert.AreEqual(2, retrievedDepositAddressList.Count);

            Assert.AreEqual(deposit.BitcoinAddress.Value, retrievedDepositAddressList[0].BitcoinAddress.Value);
            Assert.AreEqual(deposit.Status, retrievedDepositAddressList[0].Status);
            Assert.AreEqual(deposit.AccountId.Value, retrievedDepositAddressList[0].AccountId.Value);

            Assert.AreEqual(deposit.BitcoinAddress.Value, retrievedDepositAddressList[1].BitcoinAddress.Value);
            Assert.AreEqual(deposit2.Status, retrievedDepositAddressList[1].Status);
            Assert.AreEqual(deposit2.AccountId.Value, retrievedDepositAddressList[1].AccountId.Value);
        }
    }
}