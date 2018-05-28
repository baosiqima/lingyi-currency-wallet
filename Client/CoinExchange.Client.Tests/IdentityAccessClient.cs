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


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CoinExchange.Client.Tests
{
    /// <summary>
    /// Idenetity access client
    /// </summary>
    public class IdentityAccessClient : ApiClient
    {
        public IdentityAccessClient(string baseUrl) : base(baseUrl)
        {
        }

        public string SignUp(string email, string username, string password, string country, TimeZone timeZone, string pgpPublicKey)
        {
            JObject jsonObject = new JObject();
            jsonObject.Add("Email", email);
            jsonObject.Add("Username", username);
            jsonObject.Add("Country", country);
            jsonObject.Add("TimeZone", timeZone.ToString());
            jsonObject.Add("PgpPublicKey", pgpPublicKey);
            jsonObject.Add("Password", password);
            string url = _baseUrl + "/admin/signup";
            return HttpPostRequest(jsonObject, url);
        }

        public string ActivateUser(string username, string password, string activationKey)
        {
            JObject jsonObject = new JObject();
            jsonObject.Add("Username", username);
            jsonObject.Add("Password", password);
            jsonObject.Add("ActivationKey", activationKey);
            string url = _baseUrl + "/admin/user/activate";
            return HttpPostRequest(jsonObject, url);
        }

        public string Login(string username, string password)
        {
            JObject jsonObject = new JObject();
            jsonObject.Add("Username", username);
            jsonObject.Add("Password", password);
            string url = _baseUrl + "/admin/login";
            return HttpPostRequest(jsonObject, url);
        }

        public string KeyPairList()
        {
            string url = _baseUrl + "/private/user/api/list";
            return HttpGetRequest(url);
        }

        public string ListPermissions()
        {
            string url = _baseUrl + "/private/user/api/permissions";
            return HttpGetRequest(url);
        }

        public string CreateKey(string keyDescription, PermissionRepresentation[] permissions)
        {
            SecuritykeysPersmission persmission = new SecuritykeysPersmission("", "", "", false, false, false, keyDescription, permissions);
            string url = _baseUrl + "/private/user/api/create";
            return HttpPostRequest(persmission, url);
        }

        public string Logout()
        {
            string url = _baseUrl + "/private/admin/logout";
            return HttpGetRequest(url);
        }

        public string ApplyForTierLevel1(string fullName, string dateOfBirth, string phoneNumber)
        {
            JObject jsonObject = new JObject();
            jsonObject.Add("FullName", fullName);
            jsonObject.Add("DateOfBirth", dateOfBirth);
            jsonObject.Add("PhoneNumber", phoneNumber);
            string url = _baseUrl + "/private/user/applyfortier1";
            return HttpPostRequest(jsonObject, url);
        }

        public string ApplyForTierLevel2(string addressLin1, string addressLine2, string addressLine3, string state,
            string city, string zipCode)
        {
            JObject jsonObject = new JObject();
            jsonObject.Add("AddressLine1", addressLin1);
            jsonObject.Add("AddressLine2", addressLine2);
            jsonObject.Add("AddressLine3", addressLine3);
            jsonObject.Add("State", state);
            jsonObject.Add("City", city);
            jsonObject.Add("ZipCode", zipCode);
            string url = _baseUrl + "/private/user/applyfortier2";
            return HttpPostRequest(jsonObject, url);
        }

        public string ApplyForTierLevel3(string ssn, string nin, string documentType, string fileName)
        {
            JObject jsonObject = new JObject();
            jsonObject.Add("Ssn", ssn);
            jsonObject.Add("Nin", nin);
            jsonObject.Add("DocumentType", documentType);
            jsonObject.Add("FileName", fileName);
            string url = _baseUrl + "/private/user/applyfortier3";
            return HttpPostRequest(jsonObject, url);
        }

        public string VerifyTierLevel(string apiKey, string tierLevel)
        {
            JObject jsonObject = new JObject();
            jsonObject.Add("ApiKey", apiKey);
            jsonObject.Add("TierLevel", tierLevel);
            string url = _baseUrl + "/private/user/verifytierlevel";
            return HttpPostRequest(jsonObject, url);
        }
    }
}
