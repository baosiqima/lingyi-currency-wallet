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
using System.Threading;
using System.Threading.Tasks;
using CoinExchange.Common.Domain.Model;
using CoinExchange.Trades.Domain.Model.CurrencyPairAggregate;
using CoinExchange.Trades.Domain.Model.OrderAggregate;
using CoinExchange.Trades.Domain.Model.OrderMatchingEngine;
using CoinExchange.Trades.Domain.Model.Services;
using CoinExchange.Trades.Domain.Model.TradeAggregate;
using CoinExchange.Trades.Infrastructure.Persistence.RavenDb;
using CoinExchange.Trades.Infrastructure.Services;
using Disruptor;
using NUnit.Framework;

namespace CoinExchange.Trades.Domain.Model.IntegrationTests
{
    [TestFixture]
    public class OutputDisruptorTests
    {
        private ManualResetEvent _manualResetEvent;
        private IEventStore _eventStore;

        [SetUp]
        public void SetUp()
        {
            //initialize journaler
            _eventStore = new RavenNEventStore(Constants.OUTPUT_EVENT_STORE);
            Journaler journaler = new Journaler(_eventStore);
            //assign journaler to disruptor as its consumer
            OutputDisruptor.InitializeDisruptor(new IEventHandler<byte[]>[] { journaler });
            _manualResetEvent = new ManualResetEvent(false);
        }

        [TearDown]
        public void TearDown()
        {
            OutputDisruptor.ShutDown();
            _eventStore.RemoveAllEvents();
        }

        [Test]
        [Category("Integration")]
        public void AddOrdersToExchange_IfOrdersAreMatching_TradeWillbeFormedAndStoredInDbAndAllEventShouldBeDispatched()
        {
            bool tradeEventArrived = false;
            bool depthArrived = false;
            bool bboChangeArrived = false;
            bool orderBookArrived = false;
            Trade receivedTrade = null;
            IList<CurrencyPair> currencyPairs = new List<CurrencyPair>();
            currencyPairs.Add(new CurrencyPair("BTCLTC", "LTC", "BTC"));
            currencyPairs.Add(new CurrencyPair("BTCDOGE", "DOGE", "BTC"));
            Exchange exchange=new Exchange(currencyPairs);
            string currencyPair = "BTCLTC";
            Order buyOrder = OrderFactory.CreateOrder("1234", currencyPair, "limit", "buy", 5, 10,
               new StubbedOrderIdGenerator());
            Order sellOrder = OrderFactory.CreateOrder("12334", currencyPair, "market", "sell", 5, 0,
               new StubbedOrderIdGenerator());
            exchange.PlaceNewOrder(buyOrder);
            exchange.PlaceNewOrder(sellOrder);
            //receive dispatch event.
            TradeEvent.TradeOccured += delegate(Trade trade)
            {
                tradeEventArrived = true;
                receivedTrade = trade;
                //_manualResetEvent.Set();
            };
            DepthEvent.DepthChanged += delegate(Depth depth)
            {
                depthArrived = true;
            };
            BBOEvent.BBOChanged += delegate(BBO bbo)
            {
                bboChangeArrived = true;
            };
            LimitOrderBookEvent.LimitOrderBookChanged += delegate(LimitOrderBook orderBook)
            {
                orderBookArrived = true;
            };
            _manualResetEvent.WaitOne(5000);
            IList<Trade> trades = _eventStore.GetTradeEventsFromOrderId(buyOrder.OrderId.Id.ToString());

            //assert events fired
            Assert.True(tradeEventArrived);
            Assert.True(depthArrived);
            Assert.True(bboChangeArrived);
            Assert.True(orderBookArrived);

            //assert that trade event is stored in DB
            Assert.AreEqual(trades.Count,1);
            AreEqual(trades[0].BuyOrder, buyOrder);
            AreEqual(trades[0].SellOrder, sellOrder);

            //assert received trade and stored trade are same
            Assert.NotNull(receivedTrade);
            AreEqual(trades[0],receivedTrade);
        }

        /// <summary>
        /// Verify each attribute or order
        /// </summary>
        /// <param name="expected"></param>
        /// <param name="actual"></param>
        private void AreEqual(Order expected, Order actual)
        {
            Assert.AreEqual(expected.OrderId,actual.OrderId);
            Assert.AreEqual(expected.OrderSide,actual.OrderSide);
            Assert.AreEqual(expected.OrderType, actual.OrderType);
            Assert.AreEqual(expected.Volume, actual.Volume);
            Assert.AreEqual(expected.CurrencyPair, actual.CurrencyPair); 
            Assert.AreEqual(expected.TraderId, actual.TraderId);
        }

        /// <summary>
        /// Verify each attribute of trade.
        /// </summary>
        /// <param name="expected"></param>
        /// <param name="actual"></param>
        private void AreEqual(Trade expected, Trade actual)
        {
            Assert.AreEqual(expected.TradeId, actual.TradeId);
            Assert.AreEqual(expected.ExecutedVolume, actual.ExecutedVolume);
            Assert.AreEqual(expected.CurrencyPair, actual.CurrencyPair);
            Assert.AreEqual(expected.ExecutionTime.ToString(), actual.ExecutionTime.ToString());
            Assert.AreEqual(expected.ExecutionPrice, actual.ExecutionPrice);
        }

    }
}
