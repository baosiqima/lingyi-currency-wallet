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
using CoinExchange.Trades.Domain.Model.OrderAggregate;

namespace CoinExchange.Trades.ReadModel.DTO
{
    /// <summary>
    /// OrderReadModel
    /// </summary>
    public class OrderReadModel
    {
        #region properties

        public string OrderId { get; private set; }
        public string Type { get; private set; }
        public string Side { get; private set; }
        public decimal Price { get; private set; }
        public decimal VolumeExecuted { get; private set; }
        public decimal Volume { get; private set; }
        public decimal OpenQuantity { get; private set; }
        public string Status { get; private set; }
        public string TraderId { get; private set; }
        public string CurrencyPair { get; private set; }
        public DateTime DateTime { get; private set; }
        public DateTime? ClosingDateTime { get;  set; }
        public decimal AveragePrice { get;  set; }
        public IList<object> Trades { get; set; }

        #endregion

        /// <summary>
        /// Default Constructor
        /// </summary>
        public OrderReadModel()
        {
            
        }

        public OrderReadModel(string orderId, string orderType, string orderSide, decimal price, decimal volumeExecuted, string traderId, string status, string currencyPair,DateTime dateTime,decimal volume,decimal openQuantity, DateTime? closingDateTime)
        {
            OrderId = orderId;
            Type = orderType;
            Side = orderSide;
            Price = price;
            VolumeExecuted = volumeExecuted;
            TraderId = traderId;
            Status = status;
            CurrencyPair = currencyPair;
            DateTime = dateTime;
            Volume = volume;
            OpenQuantity = openQuantity;
            ClosingDateTime = closingDateTime;
        }
        
    }
}
