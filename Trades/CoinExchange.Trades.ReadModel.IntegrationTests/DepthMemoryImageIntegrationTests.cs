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
using CoinExchange.Trades.Infrastructure.Persistence.RavenDb;
using CoinExchange.Trades.Infrastructure.Services;
using CoinExchange.Trades.ReadModel.MemoryImages;
using Disruptor;
using NUnit.Framework;

namespace CoinExchange.Trades.ReadModel.IntegrationTests
{
    [TestFixture]
    class DepthMemoryImageIntegrationTests
    {
        private const string Integration = "Integration";
        private IEventStore eventStore;
        private Journaler journaler;

        [SetUp]
        public void Setup()
        {
            eventStore = new RavenNEventStore(Constants.OUTPUT_EVENT_STORE);
            journaler = new Journaler(eventStore);
            OutputDisruptor.InitializeDisruptor(new IEventHandler<byte[]>[] { journaler });
        }

        [TearDown]
        public void TearDown()
        {
            OutputDisruptor.ShutDown();
            eventStore.RemoveAllEvents();
        }
        #region Disruptor Linkage Tests

        [Test]
        [Category(Integration)]
        public void NewOrderDepthUpdatetest_ChecksWhetherDepthMemeoryImageGetsUpdatedWhenOrdersAreInserted_VerifiesThroughMemoryImagesLists()
        {
            DepthMemoryImage depthMemoryImage = new DepthMemoryImage();
            // Initialize the output Disruptor and assign the journaler as the event handler
            //IEventStore eventStore = new RavenNEventStore(Constants.OUTPUT_EVENT_STORE);
            //Journaler journaler = new Journaler(eventStore);
            //OutputDisruptor.InitializeDisruptor(new IEventHandler<byte[]>[] { journaler });

            IList<CurrencyPair> currencyPairs = new List<CurrencyPair>();
            currencyPairs.Add(new CurrencyPair("BTCUSD", "USD", "BTC"));
            currencyPairs.Add(new CurrencyPair("BTCLTC", "LTC", "BTC"));
            currencyPairs.Add(new CurrencyPair("BTCDOGE", "DOGE", "BTC"));
            // Start exchagne to accept orders
            Exchange exchange = new Exchange(currencyPairs);
            Order buyOrder1 = OrderFactory.CreateOrder("1233", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 250, 493.34M, new StubbedOrderIdGenerator());
            Order buyOrder2 = OrderFactory.CreateOrder("1234", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 100, 491.34M, new StubbedOrderIdGenerator());
            Order buyOrder3 = OrderFactory.CreateOrder("123498", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 100, 491.34M, new StubbedOrderIdGenerator());
            Order buyOrder4 = OrderFactory.CreateOrder("12355", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 100, 493.34M, new StubbedOrderIdGenerator());
            Order buyOrder5 = OrderFactory.CreateOrder("12356", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 500, 491.34M, new StubbedOrderIdGenerator());

            Order sellOrder1 = OrderFactory.CreateOrder("1244", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 250, 496.34M, new StubbedOrderIdGenerator());
            Order sellOrder2 = OrderFactory.CreateOrder("1222", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 200, 494.34M, new StubbedOrderIdGenerator());
            Order sellOrder3 = OrderFactory.CreateOrder("1264", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 200, 494.34M, new StubbedOrderIdGenerator());
            Order sellOrder4 = OrderFactory.CreateOrder("12387", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 200, 496.34M, new StubbedOrderIdGenerator());
            Order sellOrder5 = OrderFactory.CreateOrder("123897", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 200, 494.34M, new StubbedOrderIdGenerator());

            // No matching orders till now
            exchange.PlaceNewOrder(buyOrder1);
            exchange.PlaceNewOrder(buyOrder2);
            exchange.PlaceNewOrder(buyOrder3);
            exchange.PlaceNewOrder(buyOrder4);
            exchange.PlaceNewOrder(buyOrder5);
            exchange.PlaceNewOrder(sellOrder1);
            exchange.PlaceNewOrder(sellOrder2);
            exchange.PlaceNewOrder(sellOrder3);
            exchange.PlaceNewOrder(sellOrder4);
            exchange.PlaceNewOrder(sellOrder5);

            // Takes some time for the disruptor to broadcast changes to the memory image
            ManualResetEvent manualResetEvent = new ManualResetEvent(false);
            manualResetEvent.WaitOne(4000);

            Assert.AreEqual(5, exchange.ExchangeEssentials.First().LimitOrderBook.Bids.Count());
            Assert.AreEqual(5, exchange.ExchangeEssentials.First().LimitOrderBook.Asks.Count());

            // Number of currencies in the depth memory image that contain depth
            Assert.AreEqual(1, depthMemoryImage.BidDepths.Count());
            Assert.AreEqual(1, depthMemoryImage.AskDepths.Count());

            // The first pair of currencies in the bid and ask depth book, is the one that is expected
            Assert.AreEqual(CurrencyConstants.BtcUsd, depthMemoryImage.BidDepths.First().Key);
            Assert.AreEqual(CurrencyConstants.BtcUsd, depthMemoryImage.AskDepths.First().Key);

            // Count of the depth levels for each currency
            Assert.AreEqual(2, depthMemoryImage.BidDepths.First().Value.Count());
            Assert.AreEqual(2, depthMemoryImage.AskDepths.First().Value.Count());
            
            // Values of the prices and volumes and order counts in each depth level for each curernc pair's bid and ask depth
            // Vlaues at first depth level price in bids
            Assert.AreEqual(493.34M, depthMemoryImage.BidDepths.First().Value.ToList()[0].Price); // Price
            Assert.AreEqual(350, depthMemoryImage.BidDepths.First().Value.ToList()[0].Volume); // Volume
            Assert.AreEqual(2, depthMemoryImage.BidDepths.First().Value.ToList()[0].OrderCount); // Number of orders

            // Values at second depth price in bids
            Assert.AreEqual(491.34M, depthMemoryImage.BidDepths.First().Value.ToList()[1].Price);
            Assert.AreEqual(700, depthMemoryImage.BidDepths.First().Value.ToList()[1].Volume);
            Assert.AreEqual(3, depthMemoryImage.BidDepths.First().Value.ToList()[1].OrderCount);

            // Values at second depth price in asks
            Assert.AreEqual(494.34M, depthMemoryImage.AskDepths.First().Value.ToList()[0].Price);
            Assert.AreEqual(600, depthMemoryImage.AskDepths.First().Value.ToList()[0].Volume);
            Assert.AreEqual(3, depthMemoryImage.AskDepths.First().Value.ToList()[0].OrderCount);

            Assert.AreEqual(496.34M, depthMemoryImage.AskDepths.First().Value.ToList()[1].Price);
            Assert.AreEqual(450, depthMemoryImage.AskDepths.First().Value.ToList()[1].Volume);
            Assert.AreEqual(2, depthMemoryImage.AskDepths.First().Value.ToList()[1].OrderCount);

            OutputDisruptor.ShutDown();
        }

        [Test]
        [Category(Integration)]
        public void SellOrderMatctest_InCaseOfIncomingSellMatchChecksIfDepthGetsUpdatedAtMemoryImage_VerifiesThroughMemoryImagesLists()
        {
            DepthMemoryImage depthMemoryImage = new DepthMemoryImage();
            // Initialize the output Disruptor and assign the journaler as the event handler
           // IEventStore eventStore = new RavenNEventStore(Constants.OUTPUT_EVENT_STORE);
           // Journaler journaler = new Journaler(eventStore);
            //OutputDisruptor.InitializeDisruptor(new IEventHandler<byte[]>[] { journaler });

            IList<CurrencyPair> currencyPairs = new List<CurrencyPair>();
            currencyPairs.Add(new CurrencyPair("BTCUSD", "USD", "BTC"));
            currencyPairs.Add(new CurrencyPair("BTCLTC", "LTC", "BTC"));
            currencyPairs.Add(new CurrencyPair("BTCDOGE", "DOGE", "BTC"));
            // Start exchange to accept orders
            Exchange exchange = new Exchange(currencyPairs);
            Order buyOrder1 = OrderFactory.CreateOrder("1233", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 250, 493.34M, new StubbedOrderIdGenerator());
            Order buyOrder2 = OrderFactory.CreateOrder("1234", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 100, 491.34M, new StubbedOrderIdGenerator());
            Order buyOrder3 = OrderFactory.CreateOrder("123498", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 100, 491.34M, new StubbedOrderIdGenerator());
            Order buyOrder4 = OrderFactory.CreateOrder("12355", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 100, 493.34M, new StubbedOrderIdGenerator());
            Order buyOrder5 = OrderFactory.CreateOrder("12356", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 500, 491.34M, new StubbedOrderIdGenerator());

            Order sellOrder1 = OrderFactory.CreateOrder("1244", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 250, 496.34M, new StubbedOrderIdGenerator());
            Order sellOrder2 = OrderFactory.CreateOrder("1222", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 200, 494.34M, new StubbedOrderIdGenerator());
            Order sellOrder3 = OrderFactory.CreateOrder("1264", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 200, 494.34M, new StubbedOrderIdGenerator());
            Order sellOrder4 = OrderFactory.CreateOrder("12387", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 200, 496.34M, new StubbedOrderIdGenerator());
            Order sellOrder5 = OrderFactory.CreateOrder("123897", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 350, 493.34M, new StubbedOrderIdGenerator());

            // No matching orders till now
            exchange.PlaceNewOrder(buyOrder1);
            exchange.PlaceNewOrder(buyOrder2);
            exchange.PlaceNewOrder(buyOrder3);
            exchange.PlaceNewOrder(buyOrder4);
            exchange.PlaceNewOrder(buyOrder5);
            exchange.PlaceNewOrder(sellOrder1);
            exchange.PlaceNewOrder(sellOrder2);
            exchange.PlaceNewOrder(sellOrder3);
            exchange.PlaceNewOrder(sellOrder4);
            
            // Takes some time for the disruptor to broadcast changes to the memory image
            ManualResetEvent manualResetEvent = new ManualResetEvent(false);
            manualResetEvent.WaitOne(4000);

            Assert.AreEqual(5, exchange.ExchangeEssentials.First().LimitOrderBook.Bids.Count());
            Assert.AreEqual(4, exchange.ExchangeEssentials.First().LimitOrderBook.Asks.Count());

            // Number of currencies in the depth memory image that contain depth
            Assert.AreEqual(1, depthMemoryImage.BidDepths.Count());
            Assert.AreEqual(1, depthMemoryImage.AskDepths.Count());

            // The first pair of currencies in the bid and ask depth book, is the one that is expected
            Assert.AreEqual(CurrencyConstants.BtcUsd, depthMemoryImage.BidDepths.First().Key);
            Assert.AreEqual(CurrencyConstants.BtcUsd, depthMemoryImage.AskDepths.First().Key);

            // Count of the depth levels for each currency
            Assert.AreEqual(2, depthMemoryImage.BidDepths.First().Value.Count());
            Assert.AreEqual(2, depthMemoryImage.AskDepths.First().Value.Count());

            // Values of the prices and volumes and order counts in each depth level for each curernc pair's bid and ask depth
            // Vlaues at first depth level price in bids
            Assert.AreEqual(493.34M, depthMemoryImage.BidDepths.First().Value.ToList()[0].Price); // Price
            Assert.AreEqual(350, depthMemoryImage.BidDepths.First().Value.ToList()[0].Volume); // Volume
            Assert.AreEqual(2, depthMemoryImage.BidDepths.First().Value.ToList()[0].OrderCount); // Number of orders

            // Values at second depth price in bids
            Assert.AreEqual(491.34M, depthMemoryImage.BidDepths.First().Value.ToList()[1].Price);
            Assert.AreEqual(700, depthMemoryImage.BidDepths.First().Value.ToList()[1].Volume);
            Assert.AreEqual(3, depthMemoryImage.BidDepths.First().Value.ToList()[1].OrderCount);

            // Values at first depth price in asks
            Assert.AreEqual(494.34M, depthMemoryImage.AskDepths.First().Value.ToList()[0].Price);
            Assert.AreEqual(400, depthMemoryImage.AskDepths.First().Value.ToList()[0].Volume);
            Assert.AreEqual(2, depthMemoryImage.AskDepths.First().Value.ToList()[0].OrderCount);

            // Values at second depth price in asks
            Assert.AreEqual(496.34M, depthMemoryImage.AskDepths.First().Value.ToList()[1].Price);
            Assert.AreEqual(450, depthMemoryImage.AskDepths.First().Value.ToList()[1].Volume);
            Assert.AreEqual(2, depthMemoryImage.AskDepths.First().Value.ToList()[1].OrderCount);

            // Place a matching sell order to fill buy orders at 491.34
            exchange.PlaceNewOrder(sellOrder5);

            manualResetEvent.Reset();
            manualResetEvent.WaitOne(2000);

            Assert.AreEqual(3, exchange.ExchangeEssentials.First().LimitOrderBook.Bids.Count());
            Assert.AreEqual(4, exchange.ExchangeEssentials.First().LimitOrderBook.Asks.Count());

            // Number of currencies in the depth memory image that contain depth
            Assert.AreEqual(1, depthMemoryImage.BidDepths.Count());
            Assert.AreEqual(1, depthMemoryImage.AskDepths.Count());

            // The first pair of currencies in the bid and ask depth book, is the one that is expected
            Assert.AreEqual(CurrencyConstants.BtcUsd, depthMemoryImage.BidDepths.First().Key);
            Assert.AreEqual(CurrencyConstants.BtcUsd, depthMemoryImage.AskDepths.First().Key);

            // Only one bid level remains as the level at 491.34 got filled by the incoming sell match
            Assert.AreEqual(1, depthMemoryImage.BidDepths.First().Value.Count());
            Assert.AreEqual(2, depthMemoryImage.AskDepths.First().Value.Count());

            // Values of the prices and volumes and order counts in each depth level for each curernc pair's bid and ask depth
            // Values at first depth level price in bids        
            Assert.AreEqual(491.34M, depthMemoryImage.BidDepths.First().Value.ToList()[0].Price);
            Assert.AreEqual(700, depthMemoryImage.BidDepths.First().Value.ToList()[0].Volume);
            Assert.AreEqual(3, depthMemoryImage.BidDepths.First().Value.ToList()[0].OrderCount);

            // Values at first depth level price in asks
            Assert.AreEqual(494.34M, depthMemoryImage.AskDepths.First().Value.ToList()[0].Price);
            Assert.AreEqual(400, depthMemoryImage.AskDepths.First().Value.ToList()[0].Volume);
            Assert.AreEqual(2, depthMemoryImage.AskDepths.First().Value.ToList()[0].OrderCount);

            Assert.AreEqual(496.34M, depthMemoryImage.AskDepths.First().Value.ToList()[1].Price);
            Assert.AreEqual(450, depthMemoryImage.AskDepths.First().Value.ToList()[1].Volume);
            Assert.AreEqual(2, depthMemoryImage.AskDepths.First().Value.ToList()[1].OrderCount);

            OutputDisruptor.ShutDown();
        }

        [Test]
        [Category(Integration)]
        public void BuyMatchOrderTest_ChecksIfDepthUpdatesAtMiWhenIncomingBuyOrderMatches_VerifiesThroughTheMiLists()
        {
            DepthMemoryImage depthMemoryImage = new DepthMemoryImage();
            // Initialize the output Disruptor and assign the journaler as the event handler
            //IEventStore eventStore = new RavenNEventStore(Constants.OUTPUT_EVENT_STORE);
            //Journaler journaler = new Journaler(eventStore);
            //OutputDisruptor.InitializeDisruptor(new IEventHandler<byte[]>[] { journaler });

            IList<CurrencyPair> currencyPairs = new List<CurrencyPair>();
            currencyPairs.Add(new CurrencyPair("BTCUSD", "USD", "BTC"));
            currencyPairs.Add(new CurrencyPair("BTCLTC", "LTC", "BTC"));
            currencyPairs.Add(new CurrencyPair("BTCDOGE", "DOGE", "BTC"));
            // Start exchange to accept orders
            Exchange exchange = new Exchange(currencyPairs);
            Order buyOrder1 = OrderFactory.CreateOrder("1233", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 250, 493.34M, new StubbedOrderIdGenerator());
            Order buyOrder2 = OrderFactory.CreateOrder("1234", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 100, 491.34M, new StubbedOrderIdGenerator());
            Order buyOrder3 = OrderFactory.CreateOrder("123498", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 100, 491.34M, new StubbedOrderIdGenerator());
            Order buyOrder4 = OrderFactory.CreateOrder("12355", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 100, 493.34M, new StubbedOrderIdGenerator());
            Order buyOrder5 = OrderFactory.CreateOrder("12356", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 700, 494.34M, new StubbedOrderIdGenerator());

            Order sellOrder1 = OrderFactory.CreateOrder("1244", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 250, 496.34M, new StubbedOrderIdGenerator());
            Order sellOrder2 = OrderFactory.CreateOrder("1222", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 200, 494.34M, new StubbedOrderIdGenerator());
            Order sellOrder3 = OrderFactory.CreateOrder("1264", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 200, 494.34M, new StubbedOrderIdGenerator());
            Order sellOrder4 = OrderFactory.CreateOrder("12387", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 200, 496.34M, new StubbedOrderIdGenerator());
            Order sellOrder5 = OrderFactory.CreateOrder("123897", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 300, 494.34M, new StubbedOrderIdGenerator());

            // No matching orders till now
            exchange.PlaceNewOrder(buyOrder1);
            exchange.PlaceNewOrder(buyOrder2);
            exchange.PlaceNewOrder(buyOrder3);
            exchange.PlaceNewOrder(buyOrder4);
            exchange.PlaceNewOrder(sellOrder1);
            exchange.PlaceNewOrder(sellOrder2);
            exchange.PlaceNewOrder(sellOrder3);
            exchange.PlaceNewOrder(sellOrder4);
            exchange.PlaceNewOrder(sellOrder5);

            // Takes some time for the disruptor to broadcast changes to the memory image
            ManualResetEvent manualResetEvent = new ManualResetEvent(false);
            manualResetEvent.WaitOne(4000);

            Assert.AreEqual(4, exchange.ExchangeEssentials.First().LimitOrderBook.Bids.Count());
            Assert.AreEqual(5, exchange.ExchangeEssentials.First().LimitOrderBook.Asks.Count());

            // Number of currencies in the depth memory image that contain depth
            Assert.AreEqual(1, depthMemoryImage.BidDepths.Count());
            Assert.AreEqual(1, depthMemoryImage.AskDepths.Count());

            // The first pair of currencies in the bid and ask depth book, is the one that is expected
            Assert.AreEqual(CurrencyConstants.BtcUsd, depthMemoryImage.BidDepths.First().Key);
            Assert.AreEqual(CurrencyConstants.BtcUsd, depthMemoryImage.AskDepths.First().Key);

            // Count of the depth levels for each currency
            Assert.AreEqual(2, depthMemoryImage.BidDepths.First().Value.Count());
            Assert.AreEqual(2, depthMemoryImage.AskDepths.First().Value.Count());

            // Values of the prices and volumes and order counts in each depth level for each curernc pair's bid and ask depth
            // Vlaues at first depth level price in bids
            Assert.AreEqual(493.34M, depthMemoryImage.BidDepths.First().Value.ToList()[0].Price); // Price
            Assert.AreEqual(350, depthMemoryImage.BidDepths.First().Value.ToList()[0].Volume); // Volume
            Assert.AreEqual(2, depthMemoryImage.BidDepths.First().Value.ToList()[0].OrderCount); // Number of orders

            // Values at second depth price in bids
            Assert.AreEqual(491.34M, depthMemoryImage.BidDepths.First().Value.ToList()[1].Price);
            Assert.AreEqual(200, depthMemoryImage.BidDepths.First().Value.ToList()[1].Volume);
            Assert.AreEqual(2, depthMemoryImage.BidDepths.First().Value.ToList()[1].OrderCount);

            // Values at first depth price in asks
            Assert.AreEqual(494.34M, depthMemoryImage.AskDepths.First().Value.ToList()[0].Price);
            Assert.AreEqual(700, depthMemoryImage.AskDepths.First().Value.ToList()[0].Volume);
            Assert.AreEqual(3, depthMemoryImage.AskDepths.First().Value.ToList()[0].OrderCount);

            // Values at second depth price in asks
            Assert.AreEqual(496.34M, depthMemoryImage.AskDepths.First().Value.ToList()[1].Price);
            Assert.AreEqual(450, depthMemoryImage.AskDepths.First().Value.ToList()[1].Volume);
            Assert.AreEqual(2, depthMemoryImage.AskDepths.First().Value.ToList()[1].OrderCount);

            // Place a matching sell order to fill buy orders at 491.34
            exchange.PlaceNewOrder(buyOrder5);

            manualResetEvent.Reset();
            manualResetEvent.WaitOne(2000);

            Assert.AreEqual(4, exchange.ExchangeEssentials.First().LimitOrderBook.Bids.Count());
            Assert.AreEqual(2, exchange.ExchangeEssentials.First().LimitOrderBook.Asks.Count());

            // Number of currencies in the depth memory image that contain depth
            Assert.AreEqual(1, depthMemoryImage.BidDepths.Count());
            Assert.AreEqual(1, depthMemoryImage.AskDepths.Count());

            // The first pair of currencies in the bid and ask depth book, is the one that is expected
            Assert.AreEqual(CurrencyConstants.BtcUsd, depthMemoryImage.BidDepths.First().Key);
            Assert.AreEqual(CurrencyConstants.BtcUsd, depthMemoryImage.AskDepths.First().Key);

            // Count of the depth levels for each currency
            Assert.AreEqual(2, depthMemoryImage.BidDepths.First().Value.Count());
            Assert.AreEqual(1, depthMemoryImage.AskDepths.First().Value.Count());

            // Values of the prices and volumes and order counts in each depth level for each curernc pair's bid and ask depth
            // Vlaues at first depth level price in bids
            Assert.AreEqual(493.34M, depthMemoryImage.BidDepths.First().Value.ToList()[0].Price); // Price
            Assert.AreEqual(350, depthMemoryImage.BidDepths.First().Value.ToList()[0].Volume); // Volume
            Assert.AreEqual(2, depthMemoryImage.BidDepths.First().Value.ToList()[0].OrderCount); // Number of orders

            // Values at second depth price in bids
            Assert.AreEqual(491.34M, depthMemoryImage.BidDepths.First().Value.ToList()[1].Price);
            Assert.AreEqual(200, depthMemoryImage.BidDepths.First().Value.ToList()[1].Volume);
            Assert.AreEqual(2, depthMemoryImage.BidDepths.First().Value.ToList()[1].OrderCount);

            // Values at first depth price in asks
            Assert.AreEqual(496.34M, depthMemoryImage.AskDepths.First().Value.ToList()[0].Price);
            Assert.AreEqual(450, depthMemoryImage.AskDepths.First().Value.ToList()[0].Volume);
            Assert.AreEqual(2, depthMemoryImage.AskDepths.First().Value.ToList()[0].OrderCount);

            OutputDisruptor.ShutDown();
        }

        [Test]
        [Category(Integration)]
        public void MultipleSellOrderMatctest_InCaseOfIncomingSellOrdersMatchEverytimeAndDepthUpdatedForBuyAtMemoryImage_VerifiesThroughMemoryImagesLists()
        {
            DepthMemoryImage depthMemoryImage = new DepthMemoryImage();
            // Initialize the output Disruptor and assign the journaler as the event handler
            //IEventStore eventStore = new RavenNEventStore(Constants.OUTPUT_EVENT_STORE);
           // Journaler journaler = new Journaler(eventStore);
            //OutputDisruptor.InitializeDisruptor(new IEventHandler<byte[]>[] { journaler });

            IList<CurrencyPair> currencyPairs = new List<CurrencyPair>();
            currencyPairs.Add(new CurrencyPair("BTCUSD", "USD", "BTC"));
            currencyPairs.Add(new CurrencyPair("BTCLTC", "LTC", "BTC"));
            currencyPairs.Add(new CurrencyPair("BTCDOGE", "DOGE", "BTC"));
            // Start exchagne to accept orders
            Exchange exchange = new Exchange(currencyPairs);
            Order buyOrder1 = OrderFactory.CreateOrder("1233", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 250, 493.34M, new StubbedOrderIdGenerator());
            Order buyOrder2 = OrderFactory.CreateOrder("1234", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 100, 491.34M, new StubbedOrderIdGenerator());
            Order buyOrder3 = OrderFactory.CreateOrder("123498", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 100, 491.34M, new StubbedOrderIdGenerator());
            Order buyOrder4 = OrderFactory.CreateOrder("12355", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 100, 493.34M, new StubbedOrderIdGenerator());
            Order buyOrder5 = OrderFactory.CreateOrder("12356", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 500, 491.34M, new StubbedOrderIdGenerator());

            Order sellOrder1 = OrderFactory.CreateOrder("1244", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 250, 496.34M, new StubbedOrderIdGenerator());
            Order sellOrder2 = OrderFactory.CreateOrder("1222", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 200, 494.34M, new StubbedOrderIdGenerator());
            Order sellOrder3 = OrderFactory.CreateOrder("1264", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 200, 494.34M, new StubbedOrderIdGenerator());
            Order sellOrder4 = OrderFactory.CreateOrder("12387", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 200, 496.34M, new StubbedOrderIdGenerator());
            Order sellOrder5 = OrderFactory.CreateOrder("123897", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 250, 493.34M, new StubbedOrderIdGenerator());
            Order sellOrder6 = OrderFactory.CreateOrder("1238998", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 100, 493.34M, new StubbedOrderIdGenerator());

            // No matching orders till now
            exchange.PlaceNewOrder(buyOrder1);
            exchange.PlaceNewOrder(buyOrder2);
            exchange.PlaceNewOrder(buyOrder3);
            exchange.PlaceNewOrder(buyOrder4);
            exchange.PlaceNewOrder(buyOrder5);
            exchange.PlaceNewOrder(sellOrder1);
            exchange.PlaceNewOrder(sellOrder2);
            exchange.PlaceNewOrder(sellOrder3);
            exchange.PlaceNewOrder(sellOrder4);

            // Takes some time for the disruptor to broadcast changes to the memory image
            ManualResetEvent manualResetEvent = new ManualResetEvent(false);
            manualResetEvent.WaitOne(4000);

            Assert.AreEqual(5, exchange.ExchangeEssentials.First().LimitOrderBook.Bids.Count());
            Assert.AreEqual(4, exchange.ExchangeEssentials.First().LimitOrderBook.Asks.Count());

            // Number of currencies in the depth memory image that contain depth
            Assert.AreEqual(1, depthMemoryImage.BidDepths.Count());
            Assert.AreEqual(1, depthMemoryImage.AskDepths.Count());

            // The first pair of currencies in the bid and ask depth book, is the one that is expected
            Assert.AreEqual(CurrencyConstants.BtcUsd, depthMemoryImage.BidDepths.First().Key);
            Assert.AreEqual(CurrencyConstants.BtcUsd, depthMemoryImage.AskDepths.First().Key);

            // Count of the depth levels for each currency
            Assert.AreEqual(2, depthMemoryImage.BidDepths.First().Value.Count());
            Assert.AreEqual(2, depthMemoryImage.AskDepths.First().Value.Count());

            // Values of the prices and volumes and order counts in each depth level for each curernc pair's bid and ask depth
            // Vlaues at first depth level price in bids
            Assert.AreEqual(493.34M, depthMemoryImage.BidDepths.First().Value.ToList()[0].Price); // Price
            Assert.AreEqual(350, depthMemoryImage.BidDepths.First().Value.ToList()[0].Volume); // Volume
            Assert.AreEqual(2, depthMemoryImage.BidDepths.First().Value.ToList()[0].OrderCount); // Number of orders

            // Values at second depth price in bids
            Assert.AreEqual(491.34M, depthMemoryImage.BidDepths.First().Value.ToList()[1].Price);
            Assert.AreEqual(700, depthMemoryImage.BidDepths.First().Value.ToList()[1].Volume);
            Assert.AreEqual(3, depthMemoryImage.BidDepths.First().Value.ToList()[1].OrderCount);

            // Values at second depth price in asks
            Assert.AreEqual(494.34M, depthMemoryImage.AskDepths.First().Value.ToList()[0].Price);
            Assert.AreEqual(400, depthMemoryImage.AskDepths.First().Value.ToList()[0].Volume);
            Assert.AreEqual(2, depthMemoryImage.AskDepths.First().Value.ToList()[0].OrderCount);

            Assert.AreEqual(496.34M, depthMemoryImage.AskDepths.First().Value.ToList()[1].Price);
            Assert.AreEqual(450, depthMemoryImage.AskDepths.First().Value.ToList()[1].Volume);
            Assert.AreEqual(2, depthMemoryImage.AskDepths.First().Value.ToList()[1].OrderCount);

            // Place a matching sell order to fill buy orders at 491.34
            exchange.PlaceNewOrder(sellOrder5);

            manualResetEvent.Reset();
            manualResetEvent.WaitOne(2000);

            Assert.AreEqual(4, exchange.ExchangeEssentials.First().LimitOrderBook.Bids.Count());
            Assert.AreEqual(4, exchange.ExchangeEssentials.First().LimitOrderBook.Asks.Count());

            // Number of currencies in the depth memory image that contain depth
            Assert.AreEqual(1, depthMemoryImage.BidDepths.Count());
            Assert.AreEqual(1, depthMemoryImage.AskDepths.Count());

            // The first pair of currencies in the bid and ask depth book, is the one that is expected
            Assert.AreEqual(CurrencyConstants.BtcUsd, depthMemoryImage.BidDepths.First().Key);
            Assert.AreEqual(CurrencyConstants.BtcUsd, depthMemoryImage.AskDepths.First().Key);

            // Only one bid level remains as the level at 491.34 got filled by the incoming sell match
            Assert.AreEqual(2, depthMemoryImage.BidDepths.First().Value.Count());
            Assert.AreEqual(2, depthMemoryImage.AskDepths.First().Value.Count());

            // Values of the prices and volumes and order counts in each depth level for each curernc pair's bid and ask depth
            // Values at first depth level price in bids
            Assert.AreEqual(493.34M, depthMemoryImage.BidDepths.First().Value.ToList()[0].Price); // Price
            Assert.AreEqual(100, depthMemoryImage.BidDepths.First().Value.ToList()[0].Volume); // Volume
            Assert.AreEqual(1, depthMemoryImage.BidDepths.First().Value.ToList()[0].OrderCount); // Number of orders

            // Values at second depth level price in bids
            Assert.AreEqual(491.34M, depthMemoryImage.BidDepths.First().Value.ToList()[1].Price); // Price
            Assert.AreEqual(700, depthMemoryImage.BidDepths.First().Value.ToList()[1].Volume); // Volume
            Assert.AreEqual(3, depthMemoryImage.BidDepths.First().Value.ToList()[1].OrderCount); // Number of orders

            // Values at first depth price in asks
            Assert.AreEqual(494.34M, depthMemoryImage.AskDepths.First().Value.ToList()[0].Price);
            Assert.AreEqual(400, depthMemoryImage.AskDepths.First().Value.ToList()[0].Volume);
            Assert.AreEqual(2, depthMemoryImage.AskDepths.First().Value.ToList()[0].OrderCount);

            Assert.AreEqual(496.34M, depthMemoryImage.AskDepths.First().Value.ToList()[1].Price);
            Assert.AreEqual(450, depthMemoryImage.AskDepths.First().Value.ToList()[1].Volume);
            Assert.AreEqual(2, depthMemoryImage.AskDepths.First().Value.ToList()[1].OrderCount);

            // Place a matching buy order to fill sell orders
            exchange.PlaceNewOrder(sellOrder6);

            manualResetEvent.Reset();
            manualResetEvent.WaitOne(2000);

            Assert.AreEqual(3, exchange.ExchangeEssentials.First().LimitOrderBook.Bids.Count());
            Assert.AreEqual(4, exchange.ExchangeEssentials.First().LimitOrderBook.Asks.Count());

            // Number of currencies in the depth memory image that contain depth
            Assert.AreEqual(1, depthMemoryImage.BidDepths.Count());
            Assert.AreEqual(1, depthMemoryImage.AskDepths.Count());

            // The first pair of currencies in the bid and ask depth book, is the one that is expected
            Assert.AreEqual(CurrencyConstants.BtcUsd, depthMemoryImage.BidDepths.First().Key);
            Assert.AreEqual(CurrencyConstants.BtcUsd, depthMemoryImage.AskDepths.First().Key);

            // Only one bid level remains as the level at 491.34 got filled by the incoming sell match
            Assert.AreEqual(1, depthMemoryImage.BidDepths.First().Value.Count());
            Assert.AreEqual(2, depthMemoryImage.AskDepths.First().Value.Count());

            // Values of the prices and volumes and order counts in each depth level for each curernc pair's bid and ask depth
            // Values at first depth level price in bids
            Assert.AreEqual(491.34M, depthMemoryImage.BidDepths.First().Value.ToList()[0].Price); // Price
            Assert.AreEqual(700, depthMemoryImage.BidDepths.First().Value.ToList()[0].Volume); // Volume
            Assert.AreEqual(3, depthMemoryImage.BidDepths.First().Value.ToList()[0].OrderCount); // Number of orders

            // Values at first depth price in asks
            Assert.AreEqual(494.34M, depthMemoryImage.AskDepths.First().Value.ToList()[0].Price);
            Assert.AreEqual(400, depthMemoryImage.AskDepths.First().Value.ToList()[0].Volume);
            Assert.AreEqual(2, depthMemoryImage.AskDepths.First().Value.ToList()[0].OrderCount);

            // Values at second depth price in asks
            Assert.AreEqual(496.34M, depthMemoryImage.AskDepths.First().Value.ToList()[1].Price);
            Assert.AreEqual(450, depthMemoryImage.AskDepths.First().Value.ToList()[1].Volume);
            Assert.AreEqual(2, depthMemoryImage.AskDepths.First().Value.ToList()[1].OrderCount);

            OutputDisruptor.ShutDown();
        }

        [Test]
        [Category(Integration)]
        public void MultipleBuyMatchOrderTest_ChecksIfDepthUpdatesAtMiWhenMultipleIncomingBuyOrderMatches_VerifiesThroughTheMiLists()
        {
            DepthMemoryImage depthMemoryImage = new DepthMemoryImage();
            // Initialize the output Disruptor and assign the journaler as the event handler
            //IEventStore eventStore = new RavenNEventStore(Constants.OUTPUT_EVENT_STORE);
            //Journaler journaler = new Journaler(eventStore);
            //OutputDisruptor.InitializeDisruptor(new IEventHandler<byte[]>[] { journaler });

            IList<CurrencyPair> currencyPairs = new List<CurrencyPair>();
            currencyPairs.Add(new CurrencyPair("BTCUSD", "USD", "BTC"));
            currencyPairs.Add(new CurrencyPair("BTCLTC", "LTC", "BTC"));
            currencyPairs.Add(new CurrencyPair("BTCDOGE", "DOGE", "BTC"));
            // Start exchagne to accept orders
            Exchange exchange = new Exchange(currencyPairs);
            Order buyOrder1 = OrderFactory.CreateOrder("1233", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 250, 493.34M, new StubbedOrderIdGenerator());
            Order buyOrder2 = OrderFactory.CreateOrder("1234", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 100, 491.34M, new StubbedOrderIdGenerator());
            Order buyOrder3 = OrderFactory.CreateOrder("123498", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 100, 491.34M, new StubbedOrderIdGenerator());
            Order buyOrder4 = OrderFactory.CreateOrder("12355", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 100, 493.34M, new StubbedOrderIdGenerator());
            Order buyOrder5 = OrderFactory.CreateOrder("12356", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 700, 494.34M, new StubbedOrderIdGenerator());
            Order buyOrder6 = OrderFactory.CreateOrder("12356", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 250, 496.34M, new StubbedOrderIdGenerator());

            Order sellOrder1 = OrderFactory.CreateOrder("1244", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 250, 496.34M, new StubbedOrderIdGenerator());
            Order sellOrder2 = OrderFactory.CreateOrder("1222", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 200, 494.34M, new StubbedOrderIdGenerator());
            Order sellOrder3 = OrderFactory.CreateOrder("1264", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 200, 494.34M, new StubbedOrderIdGenerator());
            Order sellOrder4 = OrderFactory.CreateOrder("12387", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 200, 496.34M, new StubbedOrderIdGenerator());
            Order sellOrder5 = OrderFactory.CreateOrder("123897", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 300, 494.34M, new StubbedOrderIdGenerator());

            // No matching orders till now
            exchange.PlaceNewOrder(buyOrder1);
            exchange.PlaceNewOrder(buyOrder2);
            exchange.PlaceNewOrder(buyOrder3);
            exchange.PlaceNewOrder(buyOrder4);
            exchange.PlaceNewOrder(sellOrder1);
            exchange.PlaceNewOrder(sellOrder2);
            exchange.PlaceNewOrder(sellOrder3);
            exchange.PlaceNewOrder(sellOrder4);
            exchange.PlaceNewOrder(sellOrder5);

            // Takes some time for the disruptor to broadcast changes to the memory image
            ManualResetEvent manualResetEvent = new ManualResetEvent(false);
            manualResetEvent.WaitOne(4000);

            Assert.AreEqual(4, exchange.ExchangeEssentials.First().LimitOrderBook.Bids.Count());
            Assert.AreEqual(5, exchange.ExchangeEssentials.First().LimitOrderBook.Asks.Count());

            // Number of currencies in the depth memory image that contain depth
            Assert.AreEqual(1, depthMemoryImage.BidDepths.Count());
            Assert.AreEqual(1, depthMemoryImage.AskDepths.Count());

            // The first pair of currencies in the bid and ask depth book, is the one that is expected
            Assert.AreEqual(CurrencyConstants.BtcUsd, depthMemoryImage.BidDepths.First().Key);
            Assert.AreEqual(CurrencyConstants.BtcUsd, depthMemoryImage.AskDepths.First().Key);

            // Count of the depth levels for each currency
            Assert.AreEqual(2, depthMemoryImage.BidDepths.First().Value.Count());
            Assert.AreEqual(2, depthMemoryImage.AskDepths.First().Value.Count());

            // Values of the prices and volumes and order counts in each depth level for each curernc pair's bid and ask depth
            // Vlaues at first depth level price in bids
            Assert.AreEqual(493.34M, depthMemoryImage.BidDepths.First().Value.ToList()[0].Price); // Price
            Assert.AreEqual(350, depthMemoryImage.BidDepths.First().Value.ToList()[0].Volume); // Volume
            Assert.AreEqual(2, depthMemoryImage.BidDepths.First().Value.ToList()[0].OrderCount); // Number of orders

            // Values at second depth price in bids
            Assert.AreEqual(491.34M, depthMemoryImage.BidDepths.First().Value.ToList()[1].Price);
            Assert.AreEqual(200, depthMemoryImage.BidDepths.First().Value.ToList()[1].Volume);
            Assert.AreEqual(2, depthMemoryImage.BidDepths.First().Value.ToList()[1].OrderCount);

            // Values at first depth price in asks
            Assert.AreEqual(494.34M, depthMemoryImage.AskDepths.First().Value.ToList()[0].Price);
            Assert.AreEqual(700, depthMemoryImage.AskDepths.First().Value.ToList()[0].Volume);
            Assert.AreEqual(3, depthMemoryImage.AskDepths.First().Value.ToList()[0].OrderCount);

            // Values at second depth price in asks
            Assert.AreEqual(496.34M, depthMemoryImage.AskDepths.First().Value.ToList()[1].Price);
            Assert.AreEqual(450, depthMemoryImage.AskDepths.First().Value.ToList()[1].Volume);
            Assert.AreEqual(2, depthMemoryImage.AskDepths.First().Value.ToList()[1].OrderCount);

            // Place a matching buy order to fill sell orders
            exchange.PlaceNewOrder(buyOrder5);

            manualResetEvent.Reset();
            manualResetEvent.WaitOne(2000);

            Assert.AreEqual(4, exchange.ExchangeEssentials.First().LimitOrderBook.Bids.Count());
            Assert.AreEqual(2, exchange.ExchangeEssentials.First().LimitOrderBook.Asks.Count());

            // Number of currencies in the depth memory image that contain depth
            Assert.AreEqual(1, depthMemoryImage.BidDepths.Count());
            Assert.AreEqual(1, depthMemoryImage.AskDepths.Count());

            // The first pair of currencies in the bid and ask depth book, is the one that is expected
            Assert.AreEqual(CurrencyConstants.BtcUsd, depthMemoryImage.BidDepths.First().Key);
            Assert.AreEqual(CurrencyConstants.BtcUsd, depthMemoryImage.AskDepths.First().Key);

            // Count of the depth levels for each currency
            Assert.AreEqual(2, depthMemoryImage.BidDepths.First().Value.Count());
            Assert.AreEqual(1, depthMemoryImage.AskDepths.First().Value.Count());

            // Values of the prices and volumes and order counts in each depth level for each curernc pair's bid and ask depth
            // Vlaues at first depth level price in bids
            Assert.AreEqual(493.34M, depthMemoryImage.BidDepths.First().Value.ToList()[0].Price); // Price
            Assert.AreEqual(350, depthMemoryImage.BidDepths.First().Value.ToList()[0].Volume); // Volume
            Assert.AreEqual(2, depthMemoryImage.BidDepths.First().Value.ToList()[0].OrderCount); // Number of orders

            // Values at second depth price in bids
            Assert.AreEqual(491.34M, depthMemoryImage.BidDepths.First().Value.ToList()[1].Price);
            Assert.AreEqual(200, depthMemoryImage.BidDepths.First().Value.ToList()[1].Volume);
            Assert.AreEqual(2, depthMemoryImage.BidDepths.First().Value.ToList()[1].OrderCount);

            // Values at first depth price in asks
            Assert.AreEqual(496.34M, depthMemoryImage.AskDepths.First().Value.ToList()[0].Price);
            Assert.AreEqual(450, depthMemoryImage.AskDepths.First().Value.ToList()[0].Volume);
            Assert.AreEqual(2, depthMemoryImage.AskDepths.First().Value.ToList()[0].OrderCount);

            // Place a matching buy order to fill sell orders
            exchange.PlaceNewOrder(buyOrder6);

            manualResetEvent.Reset();
            manualResetEvent.WaitOne(2000);

            Assert.AreEqual(4, exchange.ExchangeEssentials.First().LimitOrderBook.Bids.Count());
            Assert.AreEqual(1, exchange.ExchangeEssentials.First().LimitOrderBook.Asks.Count());

            // Number of currencies in the depth memory image that contain depth
            Assert.AreEqual(1, depthMemoryImage.BidDepths.Count());
            Assert.AreEqual(1, depthMemoryImage.AskDepths.Count());

            // The first pair of currencies in the bid and ask depth book, is the one that is expected
            Assert.AreEqual(CurrencyConstants.BtcUsd, depthMemoryImage.BidDepths.First().Key);
            Assert.AreEqual(CurrencyConstants.BtcUsd, depthMemoryImage.AskDepths.First().Key);

            // Count of the depth levels for each currency
            Assert.AreEqual(2, depthMemoryImage.BidDepths.First().Value.Count());
            Assert.AreEqual(1, depthMemoryImage.AskDepths.First().Value.Count());

            // Values of the prices and volumes and order counts in each depth level for each curernc pair's bid and ask depth
            // Vlaues at first depth level price in bids
            Assert.AreEqual(493.34M, depthMemoryImage.BidDepths.First().Value.ToList()[0].Price); // Price
            Assert.AreEqual(350, depthMemoryImage.BidDepths.First().Value.ToList()[0].Volume); // Volume
            Assert.AreEqual(2, depthMemoryImage.BidDepths.First().Value.ToList()[0].OrderCount); // Number of orders

            // Values at second depth price in bids
            Assert.AreEqual(491.34M, depthMemoryImage.BidDepths.First().Value.ToList()[1].Price);
            Assert.AreEqual(200, depthMemoryImage.BidDepths.First().Value.ToList()[1].Volume);
            Assert.AreEqual(2, depthMemoryImage.BidDepths.First().Value.ToList()[1].OrderCount);

            // Values at first depth price in asks
            Assert.AreEqual(496.34M, depthMemoryImage.AskDepths.First().Value.ToList()[0].Price);
            Assert.AreEqual(200, depthMemoryImage.AskDepths.First().Value.ToList()[0].Volume);
            Assert.AreEqual(1, depthMemoryImage.AskDepths.First().Value.ToList()[0].OrderCount);

            OutputDisruptor.ShutDown();
        }

        [Test]
        [Category(Integration)]
        public void CancelBuyAndSellOrders_ChecksIfDepthMemoryImageGetsUpdatedWhenBuyAndSellOrdersAreCancelled_VerifiesDepthMemroyImagesLists()
        {
            DepthMemoryImage depthMemoryImage = new DepthMemoryImage();
            // Initialize the output Disruptor and assign the journaler as the event handler
            //IEventStore eventStore = new RavenNEventStore(Constants.OUTPUT_EVENT_STORE);
            //Journaler journaler = new Journaler(eventStore);
            //OutputDisruptor.InitializeDisruptor(new IEventHandler<byte[]>[] { journaler });

            IList<CurrencyPair> currencyPairs = new List<CurrencyPair>();
            currencyPairs.Add(new CurrencyPair("BTCUSD", "USD", "BTC"));
            currencyPairs.Add(new CurrencyPair("BTCLTC", "LTC", "BTC"));
            currencyPairs.Add(new CurrencyPair("BTCDOGE", "DOGE", "BTC"));
            // Start exchagne to accept orders
            Exchange exchange = new Exchange(currencyPairs);
            Order buyOrder1 = OrderFactory.CreateOrder("1233", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 200, 493.34M, new StubbedOrderIdGenerator());
            Order buyOrder2 = OrderFactory.CreateOrder("1234", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 100, 491.34M, new StubbedOrderIdGenerator());
            Order buyOrder3 = OrderFactory.CreateOrder("123498", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 100, 491.34M, new StubbedOrderIdGenerator());
            Order buyOrder4 = OrderFactory.CreateOrder("12355", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 100, 493.34M, new StubbedOrderIdGenerator());
            Order buyOrder5 = OrderFactory.CreateOrder("12356", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 500, 491.34M, new StubbedOrderIdGenerator());

            Order sellOrder1 = OrderFactory.CreateOrder("1244", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 250, 496.34M, new StubbedOrderIdGenerator());
            Order sellOrder2 = OrderFactory.CreateOrder("1222", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 200, 494.34M, new StubbedOrderIdGenerator());
            Order sellOrder3 = OrderFactory.CreateOrder("1264", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 200, 494.34M, new StubbedOrderIdGenerator());
            Order sellOrder4 = OrderFactory.CreateOrder("12387", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 200, 496.34M, new StubbedOrderIdGenerator());
            Order sellOrder5 = OrderFactory.CreateOrder("123897", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 250, 494.34M, new StubbedOrderIdGenerator());

            // No matching orders till now
            exchange.PlaceNewOrder(buyOrder1);
            exchange.PlaceNewOrder(buyOrder2);
            exchange.PlaceNewOrder(buyOrder3);
            exchange.PlaceNewOrder(buyOrder4);
            exchange.PlaceNewOrder(buyOrder5);
            exchange.PlaceNewOrder(sellOrder1);
            exchange.PlaceNewOrder(sellOrder2);
            exchange.PlaceNewOrder(sellOrder3);
            exchange.PlaceNewOrder(sellOrder4);
            exchange.PlaceNewOrder(sellOrder5);

            OrderId buyOrder1Id = buyOrder1.OrderId;
            OrderId buyOrder4Id = buyOrder4.OrderId;
            OrderId sellOrder3Id = sellOrder3.OrderId;
            OrderId sellOrder2Id = sellOrder2.OrderId;
            OrderId sellOrder5Id = sellOrder5.OrderId;

            // Takes some time for the disruptor to broadcast changes to the memory image
            ManualResetEvent manualResetEvent = new ManualResetEvent(false);
            manualResetEvent.WaitOne(4000);

            Assert.AreEqual(5, exchange.ExchangeEssentials.First().LimitOrderBook.Bids.Count());
            Assert.AreEqual(5, exchange.ExchangeEssentials.First().LimitOrderBook.Asks.Count());

            // Number of currencies in the depth memory image that contain depth
            Assert.AreEqual(1, depthMemoryImage.BidDepths.Count());
            Assert.AreEqual(1, depthMemoryImage.AskDepths.Count());

            // The first pair of currencies in the bid and ask depth book, is the one that is expected
            Assert.AreEqual(CurrencyConstants.BtcUsd, depthMemoryImage.BidDepths.First().Key);
            Assert.AreEqual(CurrencyConstants.BtcUsd, depthMemoryImage.AskDepths.First().Key);

            // Count of the depth levels for each currency
            Assert.AreEqual(2, depthMemoryImage.BidDepths.First().Value.Count());
            Assert.AreEqual(2, depthMemoryImage.AskDepths.First().Value.Count());

            // Values of the prices and volumes and order counts in each depth level for each curernc pair's bid and ask depth
            // Vlaues at first depth level price in bids
            Assert.AreEqual(493.34M, depthMemoryImage.BidDepths.First().Value.ToList()[0].Price); // Price
            Assert.AreEqual(300, depthMemoryImage.BidDepths.First().Value.ToList()[0].Volume); // Volume
            Assert.AreEqual(2, depthMemoryImage.BidDepths.First().Value.ToList()[0].OrderCount); // Number of orders

            // Values at second depth price in bids
            Assert.AreEqual(491.34M, depthMemoryImage.BidDepths.First().Value.ToList()[1].Price);
            Assert.AreEqual(700, depthMemoryImage.BidDepths.First().Value.ToList()[1].Volume);
            Assert.AreEqual(3, depthMemoryImage.BidDepths.First().Value.ToList()[1].OrderCount);

            // Values at second depth price in asks
            Assert.AreEqual(494.34M, depthMemoryImage.AskDepths.First().Value.ToList()[0].Price);
            Assert.AreEqual(650, depthMemoryImage.AskDepths.First().Value.ToList()[0].Volume);
            Assert.AreEqual(3, depthMemoryImage.AskDepths.First().Value.ToList()[0].OrderCount);

            Assert.AreEqual(496.34M, depthMemoryImage.AskDepths.First().Value.ToList()[1].Price);
            Assert.AreEqual(450, depthMemoryImage.AskDepths.First().Value.ToList()[1].Volume);
            Assert.AreEqual(2, depthMemoryImage.AskDepths.First().Value.ToList()[1].OrderCount);

            exchange.CancelOrder(new OrderCancellation(buyOrder1Id, buyOrder1.TraderId, buyOrder1.CurrencyPair));
            exchange.CancelOrder(new OrderCancellation(sellOrder2Id, sellOrder2.TraderId, sellOrder2.CurrencyPair));

            manualResetEvent.Reset();
            manualResetEvent.WaitOne(4000);

            Assert.AreEqual(4, exchange.ExchangeEssentials.First().LimitOrderBook.Bids.Count());
            Assert.AreEqual(4, exchange.ExchangeEssentials.First().LimitOrderBook.Asks.Count());

            // Number of currencies in the depth memory image that contain depth
            Assert.AreEqual(1, depthMemoryImage.BidDepths.Count());
            Assert.AreEqual(1, depthMemoryImage.AskDepths.Count());

            // The first pair of currencies in the bid and ask depth book, is the one that is expected
            Assert.AreEqual(CurrencyConstants.BtcUsd, depthMemoryImage.BidDepths.First().Key);
            Assert.AreEqual(CurrencyConstants.BtcUsd, depthMemoryImage.AskDepths.First().Key);

            // Count of the depth levels for each currency
            Assert.AreEqual(2, depthMemoryImage.BidDepths.First().Value.Count());
            Assert.AreEqual(2, depthMemoryImage.AskDepths.First().Value.Count());

            // Values of the prices and volumes and order counts in each depth level for each curernc pair's bid and ask depth
            // Vlaues at first depth level price in bids
            Assert.AreEqual(493.34M, depthMemoryImage.BidDepths.First().Value.ToList()[0].Price); // Price
            Assert.AreEqual(100, depthMemoryImage.BidDepths.First().Value.ToList()[0].Volume); // Volume
            Assert.AreEqual(1, depthMemoryImage.BidDepths.First().Value.ToList()[0].OrderCount); // Number of orders

            // Values at second depth price in bids
            Assert.AreEqual(491.34M, depthMemoryImage.BidDepths.First().Value.ToList()[1].Price);
            Assert.AreEqual(700, depthMemoryImage.BidDepths.First().Value.ToList()[1].Volume);
            Assert.AreEqual(3, depthMemoryImage.BidDepths.First().Value.ToList()[1].OrderCount);

            // Values at second depth price in asks
            Assert.AreEqual(494.34M, depthMemoryImage.AskDepths.First().Value.ToList()[0].Price);
            Assert.AreEqual(450, depthMemoryImage.AskDepths.First().Value.ToList()[0].Volume);
            Assert.AreEqual(2, depthMemoryImage.AskDepths.First().Value.ToList()[0].OrderCount);

            Assert.AreEqual(496.34M, depthMemoryImage.AskDepths.First().Value.ToList()[1].Price);
            Assert.AreEqual(450, depthMemoryImage.AskDepths.First().Value.ToList()[1].Volume);
            Assert.AreEqual(2, depthMemoryImage.AskDepths.First().Value.ToList()[1].OrderCount);

            exchange.CancelOrder(new OrderCancellation(buyOrder4Id, buyOrder4.TraderId, buyOrder4.CurrencyPair));
            exchange.CancelOrder(new OrderCancellation(sellOrder3Id, sellOrder3.TraderId, sellOrder3.CurrencyPair));
            exchange.CancelOrder(new OrderCancellation(sellOrder5Id, sellOrder5.TraderId, sellOrder5.CurrencyPair));

            manualResetEvent.Reset();
            manualResetEvent.WaitOne(4000);

            Assert.AreEqual(3, exchange.ExchangeEssentials.First().LimitOrderBook.Bids.Count());
            Assert.AreEqual(2, exchange.ExchangeEssentials.First().LimitOrderBook.Asks.Count());

            // Number of currencies in the depth memory image that contain depth
            Assert.AreEqual(1, depthMemoryImage.BidDepths.Count());
            Assert.AreEqual(1, depthMemoryImage.AskDepths.Count());

            // The first pair of currencies in the bid and ask depth book, is the one that is expected
            Assert.AreEqual(CurrencyConstants.BtcUsd, depthMemoryImage.BidDepths.First().Key);
            Assert.AreEqual(CurrencyConstants.BtcUsd, depthMemoryImage.AskDepths.First().Key);

            // Count of the depth levels for each currency
            Assert.AreEqual(1, depthMemoryImage.BidDepths.First().Value.Count());
            Assert.AreEqual(1, depthMemoryImage.AskDepths.First().Value.Count());

            // Values of the prices and volumes and order counts in each depth level for each curernc pair's bid and ask depth
            // Values at first depth level price in bids, lower depth gets shifted one level up
            Assert.AreEqual(491.34M, depthMemoryImage.BidDepths.First().Value.ToList()[0].Price);
            Assert.AreEqual(700, depthMemoryImage.BidDepths.First().Value.ToList()[0].Volume);
            Assert.AreEqual(3, depthMemoryImage.BidDepths.First().Value.ToList()[0].OrderCount);

            // Values at first depth price in asks, lower depth gets shifted one level up
            Assert.AreEqual(496.34M, depthMemoryImage.AskDepths.First().Value.ToList()[0].Price);
            Assert.AreEqual(450, depthMemoryImage.AskDepths.First().Value.ToList()[0].Volume);
            Assert.AreEqual(2, depthMemoryImage.AskDepths.First().Value.ToList()[0].OrderCount);

            OutputDisruptor.ShutDown();
        }

        #endregion Disruptor Linkage Tests
    }
}
