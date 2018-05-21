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
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using CoinExchange.IdentityAccess.Application.SecurityKeysServices.Commands;
using CoinExchange.IdentityAccess.Application.SecurityKeysServices.Representations;
using CoinExchange.IdentityAccess.Domain.Model.Repositories;
using CoinExchange.IdentityAccess.Domain.Model.SecurityKeysAggregate;
using CoinExchange.IdentityAccess.Domain.Model.UserAggregate;

namespace CoinExchange.IdentityAccess.Application.SecurityKeysServices
{
    /// <summary>
    /// Serves operations related to the Digital Signature(API Secret Key pair)
    /// </summary>
    public class SecurityKeysApplicationService : ISecurityKeysApplicationService
    {
        private ISecurityKeysGenerationService _securityKeysGenerationService;
        private IIdentityAccessPersistenceRepository _persistRepository;
        private ISecurityKeysRepository _securityKeysRepository;
        private IPermissionRepository _permissionRepository;
        private int _keyDescriptionCounter = 0;

        /// <summary>
        /// Initializes the service for operating operations for the DigitalSignatures
        /// </summary>
        public SecurityKeysApplicationService(ISecurityKeysGenerationService securityKeysGenerationService,
            IIdentityAccessPersistenceRepository persistenceRepository, ISecurityKeysRepository securityKeysRepository,IPermissionRepository permissionRepository)
        {
            _securityKeysGenerationService = securityKeysGenerationService;
            _persistRepository = persistenceRepository;
            _securityKeysRepository = securityKeysRepository;
            _permissionRepository = permissionRepository;
        }

        /// <summary>
        /// Generates a new API key and Secret Key pair
        /// </summary>
        /// <returns></returns>
        public Tuple<ApiKey, SecretKey, DateTime> CreateSystemGeneratedKey(int userId)
        {
            SecurityKeysPair keysPair = SecurityKeysPairFactory.SystemGeneratedSecurityKeyPair(userId, _securityKeysGenerationService);
            _persistRepository.SaveUpdate(keysPair);
            return new Tuple<ApiKey, SecretKey,DateTime>(new ApiKey(keysPair.ApiKey),new SecretKey(keysPair.SecretKey),keysPair.CreationDateTime);
        }

        /// <summary>
        /// create new key pair user generated
        /// </summary>
        /// <param name="command"></param>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public SecurityKeyPair CreateUserGeneratedKey(CreateUserGeneratedSecurityKeyPair command,string apiKey)
        {
            if (command.Validate())
            {
                //get security key pair for user name
                var getSecurityKeyPair = _securityKeysRepository.GetByApiKey(apiKey);
                if (getSecurityKeyPair == null)
                {
                    throw new ArgumentException("Invalid api key");
                }
                var keys = _securityKeysGenerationService.GenerateNewSecurityKeys();
                List<SecurityKeysPermission> permissions = new List<SecurityKeysPermission>();
                SecurityKeyPermissionsRepresentation[] securityKeyPermissionsRepresentations = this.GetPermissions();
                foreach (SecurityKeyPermissionsRepresentation securityKeyPermissionsRepresentation in securityKeyPermissionsRepresentations)
                {
                    // Check which permissions have been sent from the frontend that must be included with this User Generated Key
                    if (command.SecurityKeyPermissions.Contains(securityKeyPermissionsRepresentation.Permission.PermissionId))
                    {
                        securityKeyPermissionsRepresentation.Allowed = true;
                    }
                }
                for (int i = 0; i < securityKeyPermissionsRepresentations.Length; i++)
                {
                    permissions.Add(new SecurityKeysPermission(keys.Item1, securityKeyPermissionsRepresentations[i].Permission,
                        securityKeyPermissionsRepresentations[i].Allowed));
                }
                var keysPair = SecurityKeysPairFactory.UserGeneratedSecurityPair(getSecurityKeyPair.UserId,
                    command.KeyDescription,
                    keys.Item1, keys.Item2, command.EnableExpirationDate, command.ExpirationDateTime,
                    command.EnableStartDate, command.StartDateTime, command.EnableEndDate, command.EndDateTime,
                    permissions,
                    _securityKeysRepository);
                _persistRepository.SaveUpdate(keysPair);
                return new SecurityKeyPair(keys.Item1,keys.Item2);
            }
            throw new InvalidOperationException("Please assign atleast one permission.");
        }

        /// <summary>
        /// Set the permissions
        /// </summary>
        public bool UpdateSecurityKeyPair(UpdateUserGeneratedSecurityKeyPair updateCommand,string apiKey)
        {
            if (updateCommand.Validate())
            {
                //get security key pair for user name
                var getSecurityKeyPair = _securityKeysRepository.GetByApiKey(apiKey);
                if (getSecurityKeyPair == null)
                {
                    throw new ArgumentException("Invalid api key");
                }
                var keyPair = _securityKeysRepository.GetByApiKey(updateCommand.ApiKey);
                if (keyPair == null)
                {
                    throw new InvalidOperationException("Invalid Api Key");
                }
                var getKeyPair = _securityKeysRepository.GetByKeyDescriptionAndUserId(updateCommand.KeyDescritpion,
                    getSecurityKeyPair.UserId);
                //check if key description already exist
                if (getKeyPair!=null)
                {
                    if (!getKeyPair.ApiKey.Equals(updateCommand.ApiKey))
                    {
                        throw new InvalidOperationException("The key description already exist");
                    }
                }
                //update parameters
                keyPair.UpdateSecuritykeyPair(updateCommand.KeyDescritpion, updateCommand.EnableStartDate,
                    updateCommand.EnableEndDate, updateCommand.EnableExpirationDate, updateCommand.EndDateTime,
                    updateCommand.StartDateTime, updateCommand.ExpirationDateTime);

                //update permissions
                List<SecurityKeysPermission> permissions = new List<SecurityKeysPermission>();
                for (int i = 0; i < updateCommand.SecurityKeyPermissions.Length; i++)
                {
                    permissions.Add(new SecurityKeysPermission(keyPair.ApiKey,
                        updateCommand.SecurityKeyPermissions[i].Permission,
                        updateCommand.SecurityKeyPermissions[i].Allowed));
                }
                keyPair.UpdatePermissions(permissions.ToArray());
                //persist
                _persistRepository.SaveUpdate(keyPair);
                return true;
            }
            throw new InvalidOperationException("Please assign atleast one permission.");
        }

        /// <summary>
        /// Validate the given API Key
        /// </summary>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public bool ApiKeyValidation(string apiKey)
        {
            // ToDo: Get the API Key from the database
            throw new NotImplementedException();
        }

        /// <summary>
        /// delete security key pair
        /// </summary>
        /// <param name="keyDescription"></param>
        public void DeleteSecurityKeyPair(string keyDescription,string systemGeneratedApiKey)
        {
            //get security key pair for user name
            var getSecurityKeyPair = _securityKeysRepository.GetByApiKey(systemGeneratedApiKey);

            if (getSecurityKeyPair == null)
            {
                throw new ArgumentException("Invalid api key");
            }
            var keyPair = _securityKeysRepository.GetByKeyDescriptionAndUserId(keyDescription, getSecurityKeyPair.UserId);
            if (keyPair == null)
            {
                throw new InvalidOperationException("Could not find the security key pair.");
            }
            _securityKeysRepository.DeleteSecurityKeysPair(keyPair);
        }


        /// <summary>
        /// Get permissions
        /// </summary>
        /// <returns></returns>
        public SecurityKeyPermissionsRepresentation[] GetPermissions()
        {
            List<SecurityKeyPermissionsRepresentation> representations=new List<SecurityKeyPermissionsRepresentation>();
            IList<Permission> permissions = _permissionRepository.GetAllPermissions();
            for (int i = 0; i < permissions.Count; i++)
            {
                representations.Add(new SecurityKeyPermissionsRepresentation(false,permissions[i]));
            }
            return representations.ToArray();
        }

        /// <summary>
        /// Get api keys list
        /// </summary>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public object GetSecurityKeysPairList(string apiKey)
        {
            //get security key pair for user name
            var getSecurityKeyPair = _securityKeysRepository.GetByApiKey(apiKey);
            dynamic securityKeys = _securityKeysRepository.GetByUserId(getSecurityKeyPair.UserId);
            List<object> activeKeys = new List<object>();
            List<object> expiredKeys = new List<object>();
            foreach (dynamic securityKey in securityKeys)
            {
                if (securityKey.ExpirationDate != null)
                {
                    // If the key has not expired, addto the lsit that will be returned to the controller
                    if (securityKey.ExpirationDate >= DateTime.Now)
                    {
                        activeKeys.Add(securityKey);
                    }
                    // If key is expired, add to the list of expired keys that will be deleted
                    else
                    {
                        expiredKeys.Add(securityKey);
                    }
                }
                else
                {
                    activeKeys.Add(securityKey);
                }
            }

            foreach (dynamic expiredKey in expiredKeys)
            {
                SecurityKeysPair keyToDelete = _securityKeysRepository.GetByKeyDescriptionAndUserId(expiredKey.KeyDescription, getSecurityKeyPair.UserId);
                if (keyToDelete != null)
                {
                    _securityKeysRepository.DeleteSecurityKeysPair(keyToDelete);
                }
            }
            return activeKeys;
        }

        /// <summary>
        /// get details of specific api key
        /// </summary>
        /// <param name="keyDescription"></param>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public SecurityKeyRepresentation GetKeyDetails(string keyDescription, string apiKey)
        {
            //get security key pair for user name
            var getSecurityKeyPair = _securityKeysRepository.GetByApiKey(apiKey);
            var getApiKey = _securityKeysRepository.GetByKeyDescriptionAndUserId(keyDescription,
                getSecurityKeyPair.UserId);
            if (getApiKey != null)
            {
                List<SecurityKeyPermissionsRepresentation> representations =
                    new List<SecurityKeyPermissionsRepresentation>();
                SecurityKeysPermission[] permissions = getApiKey.GetAllPermissions();
                for (int i = 0; i < permissions.Length; i++)
                {
                    representations.Add(new SecurityKeyPermissionsRepresentation(permissions[i].IsAllowed,
                        permissions[i].Permission));
                }
                string expirationDate = getApiKey.EnableExpirationDate ? getApiKey.ExpirationDate.ToString() : "";
                string startDate = getApiKey.EnableStartDate ? getApiKey.StartDate.ToString() : "";
                string endDate = getApiKey.EnableEndDate ? getApiKey.EndDate.ToString() : "";
                return new SecurityKeyRepresentation(getApiKey.KeyDescription, getApiKey.ApiKey, getApiKey.SecretKey,
                    getApiKey.EnableStartDate,
                    getApiKey.EnableEndDate, getApiKey.EnableExpirationDate, endDate,
                    startDate, expirationDate, representations.ToArray());
            }
            throw new InvalidOperationException("Invalid key description");
        }
        
    }
}
