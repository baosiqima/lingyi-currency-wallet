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

namespace CoinExchange.Funds.Application.WithdrawServices.Representations
{
    /// <summary>
    /// Representation of Withdraw on application layer level
    /// </summary>
    public class WithdrawRepresentation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Object"/> class.
        /// </summary>
        public WithdrawRepresentation(string currency, string withdrawId, DateTime dateTime, string type, decimal amount, 
            decimal fee, string status, string bitcoinAddress, string transacitonId)
        {
            Currency = currency;
            WithdrawId = withdrawId;
            DateTime = dateTime;
            Type = type;
            Amount = amount;
            Fee = fee;
            Status = status;
            BitcoinAddress = bitcoinAddress;
            TransactionId = transacitonId;
        }

        /// <summary>
        /// Currency
        /// </summary>
        public string Currency { get; private set; }

        /// <summary>
        /// DepositId
        /// </summary>
        public string WithdrawId { get; private set; }

        /// <summary>
        /// Date
        /// </summary>
        public DateTime DateTime { get; private set; }

        /// <summary>
        /// Type
        /// </summary>
        public string Type { get; private set; }

        /// <summary>
        /// Amount
        /// </summary>
        public decimal Amount { get; private set; }

        /// <summary>
        /// Fee
        /// </summary>
        public decimal Fee { get; private set; }

        /// <summary>
        /// Status of the Deposit
        /// </summary>
        public string Status { get; private set; }

        /// <summary>
        /// Address
        /// </summary>
        public string BitcoinAddress { get; private set; }

        /// <summary>
        /// Transaction ID
        /// </summary>
        public string TransactionId { get; private set; }
    }
}
