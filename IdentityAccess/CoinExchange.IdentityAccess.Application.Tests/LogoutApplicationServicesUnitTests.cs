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
using System.Management.Instrumentation;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using CoinExchange.Common.Tests;
using CoinExchange.IdentityAccess.Application.AccessControlServices;
using CoinExchange.IdentityAccess.Application.AccessControlServices.Commands;
using CoinExchange.IdentityAccess.Application.SecurityKeysServices;
using CoinExchange.IdentityAccess.Domain.Model.Repositories;
using CoinExchange.IdentityAccess.Domain.Model.SecurityKeysAggregate;
using CoinExchange.IdentityAccess.Domain.Model.UserAggregate;
using CoinExchange.IdentityAccess.Infrastructure.Services;
using NUnit.Framework;
using Spring.Context;
using Spring.Context.Support;

namespace CoinExchange.IdentityAccess.Application.Tests
{
    [TestFixture]
    class 
        LogoutApplicationServicesUnitTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [TearDown]
        public void TearDown()
        {
        }

        [Test]
        [Category("Unit")]
        public void LogoutSuccessfulTest_ChecksIfTheTheUserProperlyLogsOutWhenCorrectCredentialsAreGiven_VerifiesTheReturnedValueToConfirm()
        {
            ISecurityKeysRepository securityKeysRepository = new MockSecurityKeysRepository();
            ILogoutApplicationService logoutApplicationService = new LogoutApplicationService(securityKeysRepository);

            (securityKeysRepository as MockSecurityKeysRepository).AddSecurityKeysPair(new SecurityKeysPair( 
                "123456789", "987654321", "1", 0, true));
            //UserValidationEssentials userValidationEssentials = new UserValidationEssentials(new Tuple<ApiKey, SecretKey>(
            //    new ApiKey("123456789"), new SecretKey("987654321")), new TimeSpan(0,0,0,10));
            bool logout = logoutApplicationService.Logout(new LogoutCommand("123456789"));

            Assert.IsTrue(logout);
        }

        [Test]
        [Category("Unit")]
        [ExpectedException(typeof(InstanceNotFoundException))]
        public void LogoutFailTest_ChecksIfLogoutFailsAsExpectedWhenWrongApiKeyIfGiven_VerifiesTheReturnedKeysToConfirm()
        {
            ISecurityKeysRepository securityKeysRepository = new MockSecurityKeysRepository();
            ILogoutApplicationService logoutApplicationService = new LogoutApplicationService(securityKeysRepository);

            (securityKeysRepository as MockSecurityKeysRepository).AddSecurityKeysPair(new SecurityKeysPair(
                "123456789", "987654321", "1", 0, true));
            //UserValidationEssentials userValidationEssentials = new UserValidationEssentials(new Tuple<ApiKey, SecretKey>(
            //    new ApiKey("12345678910"), new SecretKey("987654321")), new TimeSpan(0, 0, 0, 10));
            logoutApplicationService.Logout(new LogoutCommand("12345678910"));
        }

        [Test]
        [Category("Unit")]
        [ExpectedException(typeof(InvalidCredentialException))]
        public void LogoutFailTest_ChecksIfLogoutFailsWhenBlankApiKeyIsGiven_VerifiesTheReturnedKeysToConfirm()
        {
            ISecurityKeysRepository securityKeysRepository = new MockSecurityKeysRepository();
            ILogoutApplicationService logoutApplicationService = new LogoutApplicationService(securityKeysRepository);

            (securityKeysRepository as MockSecurityKeysRepository).AddSecurityKeysPair(new SecurityKeysPair(
                "123456789", "987654321", "1", 0, true));
            //UserValidationEssentials userValidationEssentials = new UserValidationEssentials(new Tuple<ApiKey, SecretKey>(
            //    new ApiKey(""), new SecretKey("987654321")), new TimeSpan(0, 0, 0, 10));
            logoutApplicationService.Logout(new LogoutCommand(""));
        }

        [Test]
        [Category("Unit")]
        [ExpectedException(typeof(InstanceNotFoundException))]
        public void LogoutFailTest_ChecksIfLogoutFailsWhenInvalidApiKeyIsGiven_VerifiesTheReturnedKeysToConfirm()
        {
            ISecurityKeysRepository securityKeysRepository = new MockSecurityKeysRepository();
            ILogoutApplicationService logoutApplicationService = new LogoutApplicationService(securityKeysRepository);

            (securityKeysRepository as MockSecurityKeysRepository).AddSecurityKeysPair(new SecurityKeysPair(
                "123456789", "987654321", "1", 0, true));
            //UserValidationEssentials userValidationEssentials = new UserValidationEssentials(new Tuple<ApiKey, SecretKey>(
            //    new ApiKey("12345"), new SecretKey("987654321")), new TimeSpan(0, 0, 0, 10));
            logoutApplicationService.Logout(new LogoutCommand("12345678129"));
        }

        [Test]
        [Category("Unit")]
        [ExpectedException(typeof(InvalidCredentialException))]
        public void LogoutFailTest_ChecksIfLogoutFailsWhenBlankSecretKeyIsGiven_VerifiesTheReturnedKeysToConfirm()
        {
            ISecurityKeysRepository securityKeysRepository = new MockSecurityKeysRepository();
            ILogoutApplicationService logoutApplicationService = new LogoutApplicationService(securityKeysRepository);

            (securityKeysRepository as MockSecurityKeysRepository).AddSecurityKeysPair(new SecurityKeysPair(
                "123456789", "987654321", "1", 0, true));
            //UserValidationEssentials userValidationEssentials = new UserValidationEssentials(new Tuple<ApiKey, SecretKey>(
            //    new ApiKey("123456789"), new SecretKey("")), new TimeSpan(0, 0, 0, 10));
            logoutApplicationService.Logout(new LogoutCommand(""));
        }
    }
}
