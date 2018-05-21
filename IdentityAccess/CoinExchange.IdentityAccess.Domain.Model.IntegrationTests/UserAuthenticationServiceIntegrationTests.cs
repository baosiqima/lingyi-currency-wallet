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
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using CoinExchange.Common.Tests;
using CoinExchange.IdentityAccess.Domain.Model.Repositories;
using CoinExchange.IdentityAccess.Domain.Model.SecurityKeysAggregate;
using CoinExchange.IdentityAccess.Domain.Model.UserAggregate;
using CoinExchange.IdentityAccess.Domain.Model.UserAggregate.AuthenticationServices;
using CoinExchange.IdentityAccess.Domain.Model.UserAggregate.AuthenticationServices.Commands;
using NUnit.Framework;
using Spring.Context;
using Spring.Context.Support;

namespace CoinExchange.IdentityAccess.Domain.Model.IntegrationTests
{
    [TestFixture]
    class UserAuthenticationServiceIntegrationTests
    {
        private IApplicationContext _applicationContext;
        private DatabaseUtility _databaseUtility;

        [SetUp]
        public void Setup()
        {
            _applicationContext = ContextRegistry.GetContext();
            var connection = ConfigurationManager.ConnectionStrings["MySql"].ToString();
            _databaseUtility = new DatabaseUtility(connection);
            _databaseUtility.Create();
            _databaseUtility.Populate();
        }

        [TearDown]
        public void TearDown()
        {
            ContextRegistry.Clear();
            _databaseUtility.Create();
        }

        #region System Generated Successful

        [Test]
        [Category("Integration")]
        public void SystemGeneratedKeysSuccessfulTest_TestsIfTheAuthenticationIsSuccessful_VerifiesThroughTheReturnedValue()
        {
            ISecurityKeysRepository securityKeysRepository =
                (ISecurityKeysRepository)ContextRegistry.GetContext()["SecurityKeysPairRepository"];
            IUserRepository userRepository = (IUserRepository)ContextRegistry.GetContext()["UserRepository"];
            IIdentityAccessPersistenceRepository persistenceRepository = 
                (IIdentityAccessPersistenceRepository)ContextRegistry.GetContext()["IdentityAccessPersistenceRepository"];

            UserAuthenticationService userAuthenticationService = new UserAuthenticationService(userRepository, securityKeysRepository, persistenceRepository);
            string nounce = userAuthenticationService.GenerateNonce();

            string apiKey = "123456789";
            string uri = "www.blancrock.com/orders/cancelorder";
            string secretKey = "1234567";
            string username = "linkinpark";
            
            // Create a user and store it in database, this is the user for whom we have created the key
            User user = new User("linkpark@rocknroll.com", username, "abc", "Pakistan", TimeZone.CurrentTimeZone, "", "");
            persistenceRepository.SaveUpdate(user);
            User userByUserName = userRepository.GetUserByUserName(username);
            // Create System generated Keys and store in repo.
            SecurityKeysPair securityKeysPair = new SecurityKeysPair(apiKey, secretKey, "1",  userByUserName.Id, true);
            persistenceRepository.SaveUpdate(securityKeysPair);
            string response = String.Format("{0}:{1}:{2}", apiKey, uri, secretKey).ToMD5Hash();
            bool authenticate = userAuthenticationService.Authenticate(new AuthenticateCommand("Rtg65s345",
                nounce, apiKey, uri, response, "1"));
            
            Assert.IsTrue(authenticate);
        }

        #endregion System Generated Successful

        #region System Generated Fails

        [Test]
        [Category("Integration")]
        [ExpectedException(typeof(InvalidOperationException))]
        public void SystemGeneratedKeysFailTest_TestsIfTheAuthenticationFailsDueToSessionTimeout_VerifiesThroughTheReturnedValue()
        {
            ISecurityKeysRepository securityKeysRepository =
                (ISecurityKeysRepository)ContextRegistry.GetContext()["SecurityKeysPairRepository"];
            IUserRepository userRepository = (IUserRepository)ContextRegistry.GetContext()["UserRepository"];
            IIdentityAccessPersistenceRepository persistenceRepository =
                (IIdentityAccessPersistenceRepository)ContextRegistry.GetContext()["IdentityAccessPersistenceRepository"];

            UserAuthenticationService userAuthenticationService = new UserAuthenticationService(userRepository, securityKeysRepository,persistenceRepository);
            string nounce = userAuthenticationService.GenerateNonce();

            string apiKey = "123456789";
            string uri = "www.blancrock.com/orders/cancelorder";
            string secretKey = "1234567";
            string username = "linkinpark";
            // Create System generated Keys and store in repo
            SecurityKeysPair securityKeysPair = new SecurityKeysPair(apiKey, secretKey, "1", 1, true);
            persistenceRepository.SaveUpdate(securityKeysPair);
            // Create a user and store it in database, this is the user for whom we have created the key
            User user = new User("linkpark@rocknroll.com", username, "abc", "Pakistan", TimeZone.CurrentTimeZone, "", "");
            user.AutoLogout = new TimeSpan(0,0,0,0);
            persistenceRepository.SaveUpdate(user);
            string response = String.Format("{0}:{1}:{2}", apiKey, uri, secretKey).ToMD5Hash();

            userAuthenticationService.Authenticate(new AuthenticateCommand("Rtg65s345", nounce, apiKey, uri, response, "1"));
        }

        [Test]
        [Category("Integration")]
        [ExpectedException(typeof(InvalidCredentialException))]
        public void SystemGeneratedKeysFailTest_TestsIfTheAuthenticationFailsDueToInvalidApiKey_VerifiesThroughTheReturnedValue()
        {
            ISecurityKeysRepository securityKeysRepository =
                (ISecurityKeysRepository)ContextRegistry.GetContext()["SecurityKeysPairRepository"];
            IUserRepository userRepository = (IUserRepository)ContextRegistry.GetContext()["UserRepository"];
            IIdentityAccessPersistenceRepository persistenceRepository =
                (IIdentityAccessPersistenceRepository)ContextRegistry.GetContext()["IdentityAccessPersistenceRepository"];

            UserAuthenticationService userAuthenticationService = new UserAuthenticationService(userRepository, securityKeysRepository,persistenceRepository);
            string nounce = userAuthenticationService.GenerateNonce();

            string apiKey = "123456789";
            string uri = "www.blancrock.com/orders/cancelorder";
            string secretKey = "1234567";
            string username = "linkinpark";
            // Create System generated Keys and store in repo
            SecurityKeysPair securityKeysPair = new SecurityKeysPair(apiKey, secretKey, "1", 1, true);
            persistenceRepository.SaveUpdate(securityKeysPair);
            // Create a user and store it in database, this is the user for whom we have created the key
            User user = new User("linkpark@rocknroll.com", username, "abc", "Pakistan", TimeZone.CurrentTimeZone, "", "");
            persistenceRepository.SaveUpdate(user);

            // Changed the API Key and provide an invalid one
            string response = String.Format("{0}:{1}:{2}", apiKey + "1", uri, secretKey).ToMD5Hash();
            userAuthenticationService.Authenticate(new AuthenticateCommand("Rtg65s345",
                                                                               nounce, apiKey, uri, response, "1"));
        }

        #endregion System Generated Fails

        #region User Generated Succeessful

        [Test]
        [Category("Integration")]
        public void UserGeneratedKeysQueryClosedOrdersSuccessfulTest_TestsIfTheAuthenticationIsSuccessful_VerifiesThroughTheReturnedValue()
        {
            ISecurityKeysRepository securityKeysRepository =
                (ISecurityKeysRepository)ContextRegistry.GetContext()["SecurityKeysPairRepository"];
            IUserRepository userRepository = (IUserRepository)ContextRegistry.GetContext()["UserRepository"];
            IIdentityAccessPersistenceRepository persistenceRepository =
                (IIdentityAccessPersistenceRepository)ContextRegistry.GetContext()["IdentityAccessPersistenceRepository"];

            UserAuthenticationService userAuthenticationService = new UserAuthenticationService(userRepository, securityKeysRepository,persistenceRepository);
            string nounce = userAuthenticationService.GenerateNonce();

            string apiKey = "123456789";
            string uri = "www.blancrock.com/orders/closedorders";
            string secretKey = "1234567";
            string username = "linkinpark";
            List<SecurityKeysPermission> permissionslist = new List<SecurityKeysPermission>();
            permissionslist.Add(new SecurityKeysPermission(apiKey, new Permission(PermissionsConstant.Query_Closed_Orders,
                "Query Closed Orders"), true));
            
            // Create a user and store it in database, this is the user for whom we have created the key
            User user = new User("linkpark@rocknroll.com", username, "abc", "Pakistan", TimeZone.CurrentTimeZone, "", "");
            persistenceRepository.SaveUpdate(user);
            // Get the user from the database, to confirm the user.Id generated by the database
            User userByUserName = userRepository.GetUserByUserName(username);
            SecurityKeysPair securityKeysPair = new SecurityKeysPair("1", apiKey, secretKey, userByUserName.Id,
                DateTime.Now.AddHours(2), DateTime.Now, DateTime.Now, DateTime.Now, false, permissionslist);
            securityKeysPair.EnableExpirationDate = true;
            persistenceRepository.SaveUpdate(securityKeysPair);
            string response = String.Format("{0}:{1}:{2}", apiKey, uri, secretKey).ToMD5Hash();
            bool authenticate = userAuthenticationService.Authenticate(new AuthenticateCommand("Rtg65s345",
                nounce, apiKey, uri, response, "1"));

            Assert.IsTrue(authenticate);
        }

        [Test]
        [Category("Integration")]
        public void UserGeneratedKeysQueryOpenOrdersSuccessfulTest_TestsIfTheAuthenticationIsSuccessful_VerifiesThroughTheReturnedValue()
        {
            ISecurityKeysRepository securityKeysRepository =
                (ISecurityKeysRepository)ContextRegistry.GetContext()["SecurityKeysPairRepository"];
            IUserRepository userRepository = (IUserRepository)ContextRegistry.GetContext()["UserRepository"];
            IIdentityAccessPersistenceRepository persistenceRepository =
                (IIdentityAccessPersistenceRepository)ContextRegistry.GetContext()["IdentityAccessPersistenceRepository"];

            UserAuthenticationService userAuthenticationService = new UserAuthenticationService(userRepository, securityKeysRepository,persistenceRepository);
            string nounce = userAuthenticationService.GenerateNonce();

            string apiKey = "123456789";
            string uri = "www.blancrock.com/orders/openorders";
            string secretKey = "1234567";
            string username = "linkinpark";
            List<SecurityKeysPermission> permissionslist = new List<SecurityKeysPermission>();
            permissionslist.Add(new SecurityKeysPermission(apiKey, new Permission(PermissionsConstant.Query_Open_Orders,
                "Query Open Orders"), true));
            
            // Create a user and store it in database, this is the user for whom we have created the key
            User user = new User("linkpark@rocknroll.com", username, "abc", "Pakistan", TimeZone.CurrentTimeZone, "", "");
            persistenceRepository.SaveUpdate(user);
            User userByUserName = userRepository.GetUserByUserName(username);

            SecurityKeysPair securityKeysPair = new SecurityKeysPair("1", apiKey, secretKey, userByUserName.Id,
                DateTime.Now.AddHours(2), DateTime.Now, DateTime.Now, DateTime.Now, false, permissionslist);
            securityKeysPair.EnableExpirationDate = true;
            persistenceRepository.SaveUpdate(securityKeysPair);
            string response = String.Format("{0}:{1}:{2}", apiKey, uri, secretKey).ToMD5Hash();
            bool authenticate = userAuthenticationService.Authenticate(new AuthenticateCommand("Rtg65s345",
                nounce, apiKey, uri, response, "1"));

            Assert.IsTrue(authenticate);
        }

        [Test]
        [Category("Integration")]
        public void UserGeneratedKeysCancelOrderSuccessfulTest_TestsIfTheAuthenticationIsSuccessful_VerifiesThroughTheReturnedValue()
        {
            ISecurityKeysRepository securityKeysRepository =
                (ISecurityKeysRepository)ContextRegistry.GetContext()["SecurityKeysPairRepository"];
            IUserRepository userRepository = (IUserRepository)ContextRegistry.GetContext()["UserRepository"];
            IIdentityAccessPersistenceRepository persistenceRepository =
                (IIdentityAccessPersistenceRepository)ContextRegistry.GetContext()["IdentityAccessPersistenceRepository"];

            UserAuthenticationService userAuthenticationService = new UserAuthenticationService(userRepository, securityKeysRepository,persistenceRepository);
            string nounce = userAuthenticationService.GenerateNonce();

            string apiKey = "123456789";
            string uri = "www.blancrock.com/orders/cancelorder";
            string secretKey = "1234567";
            string username = "linkinpark";
            List<SecurityKeysPermission> permissionslist = new List<SecurityKeysPermission>();
            permissionslist.Add(new SecurityKeysPermission(apiKey, new Permission(PermissionsConstant.Cancel_Order,
                "Query Cancel Orders"), true));
            // Create System generated Keys and store in repo
            //SecurityKeysPairFactory.UserGeneratedSecurityPair(username, "1", apiKey, secretKey, true, "",
            //    false, DateTime.Now, false, DateTime.Now, permissionslist, securityKeysRepository);

            // Create a user and store it in database, this is the user for whom we have created the key
            User user = new User("linkpark@rocknroll.com", username, "abc", "Pakistan", TimeZone.CurrentTimeZone, "", "");
            user.AutoLogout = new TimeSpan(0,0,10,0);
            persistenceRepository.SaveUpdate(user);
            // Get the user from the database, to confirm the user.Id generated by the database
            User userByUserName = userRepository.GetUserByUserName(username);
            SecurityKeysPair securityKeysPair = new SecurityKeysPair("1", apiKey, secretKey, userByUserName.Id,
                DateTime.Now.AddHours(2), DateTime.Now, DateTime.Now, DateTime.Now, false, permissionslist);
            securityKeysPair.EnableExpirationDate = true;
            persistenceRepository.SaveUpdate(securityKeysPair);
            string response = String.Format("{0}:{1}:{2}", apiKey, uri, secretKey).ToMD5Hash();
            bool authenticate = userAuthenticationService.Authenticate(new AuthenticateCommand("Rtg65s345",
                nounce, apiKey, uri, response, "1"));

            Assert.IsTrue(authenticate);
        }

        #endregion User Generated Succeessful

        #region User Generated Fails

        [Test]
        [Category("Integration")]
        [ExpectedException(typeof(InvalidOperationException))]
        public void UserGeneratedKeysQueryOpenOrdersFailTest_TestsIfTheAuthenticationFailsDueToExpirationDate_VerifiesThroughTheReturnedValue()
        {
            ISecurityKeysRepository securityKeysRepository =
                (ISecurityKeysRepository)ContextRegistry.GetContext()["SecurityKeysPairRepository"];
            IUserRepository userRepository = (IUserRepository)ContextRegistry.GetContext()["UserRepository"];
            IIdentityAccessPersistenceRepository persistenceRepository =
                (IIdentityAccessPersistenceRepository)ContextRegistry.GetContext()["IdentityAccessPersistenceRepository"];

            UserAuthenticationService userAuthenticationService = new UserAuthenticationService(userRepository, securityKeysRepository,persistenceRepository);
            string nounce = userAuthenticationService.GenerateNonce();

            string apiKey = "123456789";
            string uri = "www.blancrock.com/orders/cancelorder";
            string secretKey = "1234567";
            string username = "linkinpark";
            // Provide the permission
            List<SecurityKeysPermission> permissionslist = new List<SecurityKeysPermission>();
            permissionslist.Add(new SecurityKeysPermission(apiKey, new Permission(PermissionsConstant.Query_Open_Orders,
                "Query Open Orders"), true));
            // Here we provide the expiration date for the current moment, which will expire instantly
            SecurityKeysPair securityKeysPair = new SecurityKeysPair("1", apiKey, secretKey, 1,
                DateTime.Now, DateTime.Now, DateTime.Now, DateTime.Now, false, permissionslist);
            securityKeysPair.EnableExpirationDate = true;
            persistenceRepository.SaveUpdate(securityKeysPair);
            // Create a user and store it in database, this is the user for whom we have created the key
            User user = new User("linkpark@rocknroll.com", username, "abc", "Pakistan", TimeZone.CurrentTimeZone, "", "");
            persistenceRepository.SaveUpdate(user);
            string response = String.Format("{0}:{1}:{2}", apiKey, uri, secretKey).ToMD5Hash();

            userAuthenticationService.Authenticate(new AuthenticateCommand("Rtg65s345",
                                                                               nounce, apiKey, uri, response, "1"));
        }

        [Test]
        [Category("Integration")]
        [ExpectedException(typeof(InvalidOperationException))]
        public void UserGeneratedKeysQueryClosedOrdersFailTest_TestsIfTheAuthenticationFailsDueToExpirationDate_VerifiesThroughTheReturnedValue()
        {
            ISecurityKeysRepository securityKeysRepository =
                (ISecurityKeysRepository)ContextRegistry.GetContext()["SecurityKeysPairRepository"];
            IUserRepository userRepository = (IUserRepository)ContextRegistry.GetContext()["UserRepository"];
            IIdentityAccessPersistenceRepository persistenceRepository =
                (IIdentityAccessPersistenceRepository)ContextRegistry.GetContext()["IdentityAccessPersistenceRepository"];

            UserAuthenticationService userAuthenticationService = new UserAuthenticationService(userRepository, securityKeysRepository,persistenceRepository);
            string nounce = userAuthenticationService.GenerateNonce();

            string apiKey = "123456789";
            string uri = "www.blancrock.com/orders/cancelorder";
            string secretKey = "1234567";
            string username = "linkinpark";
            // Provide the permission
            List<SecurityKeysPermission> permissionslist = new List<SecurityKeysPermission>();
            permissionslist.Add(new SecurityKeysPermission(apiKey, new Permission(PermissionsConstant.Query_Closed_Orders,
                "Query Closed Orders"), true));
            // Here we provide the expiration date for the current moment, which will expire instantly
            SecurityKeysPair securityKeysPair = new SecurityKeysPair("1", apiKey, secretKey, 1,
                DateTime.Now, DateTime.Now, DateTime.Now, DateTime.Now, false, permissionslist);
            securityKeysPair.EnableExpirationDate = true;
            persistenceRepository.SaveUpdate(securityKeysPair);
            // Create a user and store it in database, this is the user for whom we have created the key
            User user = new User("linkpark@rocknroll.com", username, "abc", "Pakistan", TimeZone.CurrentTimeZone, "", "");
            persistenceRepository.SaveUpdate(user);
            string response = String.Format("{0}:{1}:{2}", apiKey, uri, secretKey).ToMD5Hash();

            userAuthenticationService.Authenticate(new AuthenticateCommand("Rtg65s345",
                                                                               nounce, apiKey, uri, response, "1"));
        }

        [Test]
        [Category("Integration")]
        [ExpectedException(typeof(InvalidOperationException))]
        public void UserGeneratedKeysCancelOrderFailTest_TestsIfTheAuthenticationFailsDueToExpirationDate_VerifiesThroughTheReturnedValue()
        {
            ISecurityKeysRepository securityKeysRepository =
                (ISecurityKeysRepository)ContextRegistry.GetContext()["SecurityKeysPairRepository"];
            IUserRepository userRepository = (IUserRepository)ContextRegistry.GetContext()["UserRepository"];
            IIdentityAccessPersistenceRepository persistenceRepository =
                (IIdentityAccessPersistenceRepository)ContextRegistry.GetContext()["IdentityAccessPersistenceRepository"];

            UserAuthenticationService userAuthenticationService = new UserAuthenticationService(userRepository, securityKeysRepository,persistenceRepository);
            string nounce = userAuthenticationService.GenerateNonce();

            string apiKey = "123456789";
            string uri = "www.blancrock.com/orders/cancelorder";
            string secretKey = "1234567";
            string username = "linkinpark";
            // Provide the permission
            List<SecurityKeysPermission> permissionslist = new List<SecurityKeysPermission>();
            permissionslist.Add(new SecurityKeysPermission(apiKey, new Permission(PermissionsConstant.Cancel_Order,
                "Cancel Orders"), true));
            
            // Create a user and store it in database, this is the user for whom we have created the key
            User user = new User("linkpark@rocknroll.com", username, "abc", "Pakistan", TimeZone.CurrentTimeZone, "", "");
            persistenceRepository.SaveUpdate(user);
            User userByUserName = userRepository.GetUserByUserName(username);
            // Here we provide the expiration date for the current moment, which will expire instantly
            SecurityKeysPair securityKeysPair = new SecurityKeysPair("1", apiKey, secretKey, userByUserName.Id,
                DateTime.Now, DateTime.Now, DateTime.Now, DateTime.Now, false, permissionslist);
            securityKeysPair.EnableExpirationDate = true;
            persistenceRepository.SaveUpdate(securityKeysPair);
            string response = String.Format("{0}:{1}:{2}", apiKey, uri, secretKey).ToMD5Hash();
            
            userAuthenticationService.Authenticate(new AuthenticateCommand("Rtg65s345",
                                                                               nounce, apiKey, uri, response, "1"));
        }

        [Test]
        [Category("Integration")]
        [ExpectedException(typeof(InvalidOperationException))]
        public void UserGeneratedKeysFailTest_TestsIfTheAuthenticationFailsDueInvalidUserIdinSecurityKeysPair_VerifiesThroughTheReturnedValue()
        {
            ISecurityKeysRepository securityKeysRepository =
                (ISecurityKeysRepository) ContextRegistry.GetContext()["SecurityKeysPairRepository"];
            IUserRepository userRepository = (IUserRepository) ContextRegistry.GetContext()["UserRepository"];
            IIdentityAccessPersistenceRepository persistenceRepository =
                (IIdentityAccessPersistenceRepository)
                ContextRegistry.GetContext()["IdentityAccessPersistenceRepository"];

            UserAuthenticationService userAuthenticationService = new UserAuthenticationService(userRepository,
                                                                                                securityKeysRepository,persistenceRepository);
            string nounce = userAuthenticationService.GenerateNonce();

            string apiKey = "123456789";
            string uri = "www.blancrock.com/orders/cancelorder";
            string secretKey = "1234567";
            string username = "linkinpark";
            // Provide the permission
            List<SecurityKeysPermission> permissionslist = new List<SecurityKeysPermission>();
            permissionslist.Add(new SecurityKeysPermission(apiKey, new Permission(PermissionsConstant.Query_Open_Orders,
                                                                                  "Query Open Orders"), true));
            // Create a user and store it in database, this is the user for whom we have created the key
            User user = new User("linkpark@rocknroll.com", username, "abc", "Pakistan", TimeZone.CurrentTimeZone, "", "");
            persistenceRepository.SaveUpdate(user);
            // Give invalid User.ID
            SecurityKeysPair securityKeysPair = new SecurityKeysPair("1", apiKey, secretKey, 898658947,
                                                                     DateTime.Now, DateTime.Now, DateTime.Now,
                                                                     DateTime.Now, false, permissionslist);
            securityKeysPair.EnableExpirationDate = true;
            persistenceRepository.SaveUpdate(securityKeysPair);
            string response = String.Format("{0}:{1}:{2}", apiKey, uri, secretKey).ToMD5Hash();

            userAuthenticationService.Authenticate(new AuthenticateCommand("Rtg65s345",
                                                                           nounce, apiKey, uri, response, "1"));
        }


        #endregion User Generated Fails
    }
}
