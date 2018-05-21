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
using CoinExchange.Trades.Application.MarketDataServices.Representation;
using CoinExchange.Trades.Domain.Model.MarketDataAggregate;
using CoinExchange.Trades.Domain.Model.OrderAggregate;
using CoinExchange.Trades.ReadModel.MemoryImages;

namespace CoinExchange.Trades.Application.MarketDataServices
{
    /// <summary>
    /// Service serving the iperations relatedto Market Data
    /// </summary>
    public class StubbedMarketDataQueryService : IMarketDataQueryService
    {
        /// <summary>
        /// Get ticker info
        /// </summary>
        /// <param name="pairs"></param>
        /// <returns></returns>
        public object GetTickerInfo(string pairs)
        {
            MarketData marketData = new MarketData(200, 200.33m, "Bid");
            TickerRepresentation representation = new TickerRepresentation(pairs, marketData, marketData, marketData, new decimal[] { 200, 300 },
                new decimal[] { 200, 300 }, new[] { 200, 200 }, new decimal[] { 200, 300 }, new decimal[] { 200, 300 },
                200m);
            TickerRepresentation[] representations = new TickerRepresentation[3] { representation, representation, representation };
            return representations;
        }

        /// <summary>
        /// Get Ohlc info
        /// </summary>
        /// <param name="pair"></param>
        /// <param name="interval"></param>
        /// <param name="since"></param>
        /// <returns></returns>
        public OhlcRepresentation GetOhlcInfo(string pair, int interval, string since)
        {
            Ohlc ohlcData = new Ohlc(DateTime.UtcNow, 113, 111, 123, 114, 100, 1000, 23);
            List<Ohlc> dataList = new List<Ohlc>();
            for (int i = 0; i < 10; i++)
            {
                dataList.Add(ohlcData);
            }
            OhlcRepresentation representation = new OhlcRepresentation(dataList, pair, 100);
            return representation;
        }

        /// <summary>
        /// Returns the Order Book
        /// </summary>
        /// <returns></returns>
        public object GetOrderBook(string symbol, int count)
        {
            OrderRepresentationList bidList = new OrderRepresentationList(symbol, OrderSide.Buy);
            bidList.UpdateAtIndex(0, 1000, 491.34M,DateTime.Now);
            bidList.UpdateAtIndex(1, 900, 491.11M,DateTime.Now);
            bidList.UpdateAtIndex(1, 2300, 489.11M,DateTime.Now);
            OrderRepresentationList askList = new OrderRepresentationList(symbol, OrderSide.Sell);
            askList.UpdateAtIndex(1, 900, 499.11M,DateTime.Now);
            askList.UpdateAtIndex(1, 300, 493.11M,DateTime.Now);
            askList.UpdateAtIndex(1, 2200, 492.11M,DateTime.Now);

            return new Tuple<OrderRepresentationList, OrderRepresentationList>(bidList, askList);
            // ToDo: Remove the below commented out code when this request works perfectly on UI
            /*List<object> list = new List<object>();
            list.Add(symbol);
            list.Add("asks");
            list.Add(new object[] { "23", "1000", "204832014" });
            list.Add(new object[] { "32", "1000", "204832014" });
            list.Add(new object[] { "34", "1000", "204832014" });

            list.Add("bids");
            list.Add(new object[] { "34", "1000", "204832014" });
            list.Add(new object[] { "23", "1000", "204832014" });
            list.Add(new object[] { "33", "1000", "204832014" });

            return list;*/
        }

        public object GetDepth(string currencyPair)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get Rate for the given Currency Pair
        /// </summary>
        /// <param name="currencyPair"></param>
        /// <returns></returns>
        public Rate GetRate(string currencyPair)
        {
            return new Rate(currencyPair, 495.87M);
        }

        /// <summary>
        /// Get Rates for all the currency pairs
        /// </summary>
        /// <returns></returns>
        public RatesList GetAllRates()
        {
            RatesList ratesList = new RatesList();
            ratesList.AddRate("XBT/USD", 491.34M, 495.65M);
            ratesList.AddRate("LTC/USD", 496.34M, 499.65M);

            return ratesList;
        }


        public object GetSpread(string currencyPair)
        {
            throw new NotImplementedException();
        }


        public BBORepresentation GetBBO(string currencyPair)
        {
            throw new NotImplementedException();
        }
    }
}
