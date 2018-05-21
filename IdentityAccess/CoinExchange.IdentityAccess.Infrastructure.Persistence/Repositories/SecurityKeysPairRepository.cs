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
using System.Threading.Tasks;
using CoinExchange.Common.Services;
using CoinExchange.IdentityAccess.Domain.Model.Repositories;
using CoinExchange.IdentityAccess.Domain.Model.SecurityKeysAggregate;
using CoinExchange.IdentityAccess.Domain.Model.UserAggregate;
using CoinExchange.IdentityAccess.Infrastructure.Persistence.Projection;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Transform;
using Spring.Stereotype;
using Spring.Transaction.Interceptor;

namespace CoinExchange.IdentityAccess.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Repository for DigitalSignatureInfo
    /// </summary>
    [Repository]
    public class SecurityKeysPairRepository : NHibernateSessionFactory, ISecurityKeysRepository, IApiKeyInfoAccess
    {
        [Transaction(ReadOnly = true)]
        public SecurityKeysPair GetByKeyDescriptionAndUserId(string keyDescription, int userId)
        {
            return
                CurrentSession.QueryOver<SecurityKeysPair>()
                    .Where(x => x.KeyDescription == keyDescription && x.UserId == userId && x.Deleted == false)
                    .SingleOrDefault();
        }

        [Transaction(ReadOnly = true)]
        public SecurityKeysPair GetByApiKey(string apiKey)
        {
            return
                CurrentSession.QueryOver<SecurityKeysPair>()
                    .Where(x => x.ApiKey == apiKey && x.Deleted == false)
                    .SingleOrDefault();
        }
       
        /// <summary>
        /// Soft delete security key pair.
        /// </summary>
        /// <param name="securityKeysPair"></param>
        /// <returns></returns>
        [Transaction(ReadOnly = false)]
        public bool DeleteSecurityKeysPair(SecurityKeysPair securityKeysPair)
        {
            securityKeysPair.Deleted = true;
            CurrentSession.SaveOrUpdate(securityKeysPair);
            return true;
        }

        [Transaction(ReadOnly = true)]
        public SecurityKeysPair GetByDescriptionAndApiKey(string description, string apiKey)
        {
            return
                CurrentSession.QueryOver<SecurityKeysPair>()
                    .Where(x => x.KeyDescription == description && x.ApiKey == apiKey && x.Deleted == false)
                    .SingleOrDefault();
        }

        [Transaction(ReadOnly = true)]
        public object GetByUserId(int userId)
        {
            ICriteria crit = CurrentSession.CreateCriteria<SecurityKeysPair>().Add(Restrictions.Eq("UserId", userId)).Add(Restrictions.Eq("SystemGenerated", false)).Add(Restrictions.Eq("Deleted", false))
                        .SetProjection(Projections.ProjectionList()
                            .Add(Projections.Property("KeyDescription"), "KeyDescription")
                            .Add(Projections.Property("ExpirationDate"), "ExpirationDate")
                            .Add(Projections.Property("LastModified"), "LastModified")
                            .Add(Projections.Property("CreationDateTime"), "CreationDateTime")).SetResultTransformer(Transformers.AliasToBean<SecurityKeyPairList>());
            IList<SecurityKeyPairList> results = crit.List<SecurityKeyPairList>();
            return results;
            //return CurrentSession.QueryOver<SecurityKeysPair>().Select(t=>t.KeyDescription,t=>t.ExpirationDate,t=>t.CreationDateTime,t=>t.LastModified).Where(t=>t.UserId==userId && t.Deleted==false && t.SystemGenerated==false)
            //   .List<SecurityKeyPairList>();
        }

        /// <summary>
        /// GetUserId from apikey
        /// </summary>
        /// <param name="apiKey"></param>
        [Transaction(ReadOnly = true)]
        public int GetUserIdFromApiKey(string apiKey)
        {
            return
                CurrentSession.QueryOver<SecurityKeysPair>()
                    .Where(x=>x.ApiKey == apiKey && x.Deleted == false)
                    .SingleOrDefault().UserId;
        }
    }
}
