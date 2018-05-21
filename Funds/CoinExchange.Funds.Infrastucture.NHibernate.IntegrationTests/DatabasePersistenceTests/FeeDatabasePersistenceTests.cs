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


﻿using System.Collections.Generic;
using System.Configuration;
using CoinExchange.Common.Tests;
using CoinExchange.Funds.Domain.Model.FeeAggregate;
using CoinExchange.Funds.Domain.Model.Repositories;
using NUnit.Framework;
using Spring.Context.Support;

namespace CoinExchange.Funds.Infrastucture.NHibernate.IntegrationTests.DatabasePersistenceTests
{
    /// <summary>
    /// Tests by actually persisting the Fee objects to the database
    /// </summary>
    [TestFixture]
    class FeeDatabasePersistenceTests
    {
        private DatabaseUtility _databaseUtility;
        private IFundsPersistenceRepository _persistanceRepository;
        private IFeeRepository _feeRepository;

        [SetUp]
        public void Setup()
        {
            _feeRepository = (IFeeRepository)ContextRegistry.GetContext()["FeeRepository"];
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
        public void SaveFeeAndRetreiveByCurrencyPairNameTest_SavesAnObjectToDatabaseAndManipulatesIt_ChecksIfItIsUpdatedAsExpected()
        {
            Fee fee = new Fee("LTC/BTC", 4000, 500);

            _persistanceRepository.SaveOrUpdate(fee);

            List<Fee> retrievedFee = _feeRepository.GetFeeByCurrencyPair("LTC/BTC");
            Assert.IsNotNull(retrievedFee);

            Assert.AreEqual(fee.PercentageFee, retrievedFee[0].PercentageFee);
            Assert.AreEqual(fee.Amount, retrievedFee[0].Amount);
        }

        [Test]
        public void SaveFeeAndRetreiveByIdTest_SavesAnObjectToDatabaseAndManipulatesIt_ChecksIfItIsUpdatedAsExpected()
        {
            Fee fee = new Fee("LTC/BTC", 4000, 500);

            _persistanceRepository.SaveOrUpdate(fee);

            List<Fee> retrievedFee = _feeRepository.GetFeeByCurrencyPair("LTC/BTC");
            Assert.IsNotNull(retrievedFee);
            int id = retrievedFee[0].Id;
            _persistanceRepository.SaveOrUpdate(retrievedFee);

            fee = _feeRepository.GetFeeById(id);
            Assert.AreEqual(fee.PercentageFee, fee.PercentageFee);
            Assert.AreEqual(fee.Amount, fee.Amount);
        }
    }
}
