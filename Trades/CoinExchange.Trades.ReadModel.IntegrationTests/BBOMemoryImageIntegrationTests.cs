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
    class BBOMemoryImageIntegrationTests
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
        public void AddBuyBBoTest_ChecksIfAddingnewBuysUpdatesTheBBOAtMemoryImageSide_VerifiesThroughMemoryImagesBBOList()
        {
            BBOMemoryImage bboMemoryImage = new BBOMemoryImage();

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
                Constants.ORDER_SIDE_BUY, 100, 494.34M, new StubbedOrderIdGenerator());
            Order buyOrder3 = OrderFactory.CreateOrder("123498", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 100, 494.34M, new StubbedOrderIdGenerator());
            Order buyOrder4 = OrderFactory.CreateOrder("12355", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 100, 496.34M, new StubbedOrderIdGenerator());
            Order buyOrder5 = OrderFactory.CreateOrder("12356", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 500, 497.34M, new StubbedOrderIdGenerator());

            // Initial buy. Send buys with a higher price than te last one
            exchange.PlaceNewOrder(buyOrder1);

            // Takes some time for the disruptor to broadcast changes to the memory image
            ManualResetEvent manualResetEvent = new ManualResetEvent(false);
            manualResetEvent.WaitOne(2000);

            Assert.AreEqual(1, exchange.ExchangeEssentials.First().LimitOrderBook.Bids.Count());
            Assert.AreEqual(0, exchange.ExchangeEssentials.First().LimitOrderBook.Asks.Count());

            // Number of currencies in the memory image that contain depth
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.Count());

            // Best bid and its depth stats
            Assert.AreEqual(CurrencyConstants.BtcUsd, bboMemoryImage.BBORepresentationList.First().CurrencyPair);
            Assert.AreEqual(buyOrder1.Volume.Value, bboMemoryImage.BBORepresentationList.First().BestBidVolume);
            Assert.AreEqual(buyOrder1.Price.Value, bboMemoryImage.BBORepresentationList.First().BestBidPrice);
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.First().BestBidOrderCount);

            // Best ask and its depth stats
            Assert.AreEqual(CurrencyConstants.BtcUsd, bboMemoryImage.BBORepresentationList.First().CurrencyPair);
            Assert.AreEqual(0, bboMemoryImage.BBORepresentationList.First().BestAskVolume);
            Assert.AreEqual(0, bboMemoryImage.BBORepresentationList.First().BestAskPrice);
            Assert.AreEqual(0, bboMemoryImage.BBORepresentationList.First().BestAskOrderCount);

            // Buy with higher price than the last one
            exchange.PlaceNewOrder(buyOrder2);

            // Takes some time for the disruptor to broadcast changes to the memory image
            manualResetEvent.Reset();
            manualResetEvent.WaitOne(2000);

            Assert.AreEqual(2, exchange.ExchangeEssentials.First().LimitOrderBook.Bids.Count());
            Assert.AreEqual(0, exchange.ExchangeEssentials.First().LimitOrderBook.Asks.Count());

            // Number of currencies in the memory image that contain depth
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.Count());

            // Best bid and its depth stats
            Assert.AreEqual(CurrencyConstants.BtcUsd, bboMemoryImage.BBORepresentationList.First().CurrencyPair);
            Assert.AreEqual(buyOrder2.Volume.Value, bboMemoryImage.BBORepresentationList.First().BestBidVolume);
            Assert.AreEqual(buyOrder2.Price.Value, bboMemoryImage.BBORepresentationList.First().BestBidPrice);
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.First().BestBidOrderCount);

            // Best ask and its depth stats
            Assert.AreEqual(CurrencyConstants.BtcUsd, bboMemoryImage.BBORepresentationList.First().CurrencyPair);
            Assert.AreEqual(0, bboMemoryImage.BBORepresentationList.First().BestAskVolume);
            Assert.AreEqual(0, bboMemoryImage.BBORepresentationList.First().BestAskPrice);
            Assert.AreEqual(0, bboMemoryImage.BBORepresentationList.First().BestAskOrderCount);

            // Buy with higher price than the last one
            exchange.PlaceNewOrder(buyOrder3);

            // Takes some time for the disruptor to broadcast changes to the memory image
            manualResetEvent.Reset();
            manualResetEvent.WaitOne(2000);

            Assert.AreEqual(3, exchange.ExchangeEssentials.First().LimitOrderBook.Bids.Count());
            Assert.AreEqual(0, exchange.ExchangeEssentials.First().LimitOrderBook.Asks.Count());

            // Number of currencies in the memory image that contain depth
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.Count());

            // Best bid and its depth stats
            Assert.AreEqual(CurrencyConstants.BtcUsd, bboMemoryImage.BBORepresentationList.First().CurrencyPair);
            Assert.AreEqual(200, bboMemoryImage.BBORepresentationList.First().BestBidVolume);// Now contains aggregated volume of 2 orders
            Assert.AreEqual(buyOrder2.Price.Value, bboMemoryImage.BBORepresentationList.First().BestBidPrice);
            Assert.AreEqual(2, bboMemoryImage.BBORepresentationList.First().BestBidOrderCount);// Now contains two orders

            // Best ask and its depth stats
            Assert.AreEqual(CurrencyConstants.BtcUsd, bboMemoryImage.BBORepresentationList.First().CurrencyPair);
            Assert.AreEqual(0, bboMemoryImage.BBORepresentationList.First().BestAskVolume);
            Assert.AreEqual(0, bboMemoryImage.BBORepresentationList.First().BestAskPrice);
            Assert.AreEqual(0, bboMemoryImage.BBORepresentationList.First().BestAskOrderCount);

            // Buy with higher price than the last one
            exchange.PlaceNewOrder(buyOrder4);

            // Takes some time for the disruptor to broadcast changes to the memory image
            manualResetEvent.Reset();
            manualResetEvent.WaitOne(2000);

            Assert.AreEqual(4, exchange.ExchangeEssentials.First().LimitOrderBook.Bids.Count());
            Assert.AreEqual(0, exchange.ExchangeEssentials.First().LimitOrderBook.Asks.Count());

            // Number of currencies in the memory image that contain depth
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.Count());

            // Best bid and its depth stats
            Assert.AreEqual(CurrencyConstants.BtcUsd, bboMemoryImage.BBORepresentationList.First().CurrencyPair);
            Assert.AreEqual(100, bboMemoryImage.BBORepresentationList.First().BestBidVolume);
            Assert.AreEqual(496.34M, bboMemoryImage.BBORepresentationList.First().BestBidPrice);
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.First().BestBidOrderCount);

            // Best ask and its depth stats
            Assert.AreEqual(CurrencyConstants.BtcUsd, bboMemoryImage.BBORepresentationList.First().CurrencyPair);
            Assert.AreEqual(0, bboMemoryImage.BBORepresentationList.First().BestAskVolume);
            Assert.AreEqual(0, bboMemoryImage.BBORepresentationList.First().BestAskPrice);
            Assert.AreEqual(0, bboMemoryImage.BBORepresentationList.First().BestAskOrderCount);

            // Buy with higher price than the last one
            exchange.PlaceNewOrder(buyOrder5);

            // Takes some time for the disruptor to broadcast changes to the memory image
            manualResetEvent.Reset();
            manualResetEvent.WaitOne(2000);

            Assert.AreEqual(5, exchange.ExchangeEssentials.First().LimitOrderBook.Bids.Count());
            Assert.AreEqual(0, exchange.ExchangeEssentials.First().LimitOrderBook.Asks.Count());

            // Number of currencies in the memory image that contain depth
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.Count());

            // Best bid and its depth stats
            Assert.AreEqual(CurrencyConstants.BtcUsd, bboMemoryImage.BBORepresentationList.First().CurrencyPair);
            Assert.AreEqual(500, bboMemoryImage.BBORepresentationList.First().BestBidVolume);
            Assert.AreEqual(497.34M, bboMemoryImage.BBORepresentationList.First().BestBidPrice);
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.First().BestBidOrderCount);

            // Best ask and its depth stats
            Assert.AreEqual(CurrencyConstants.BtcUsd, bboMemoryImage.BBORepresentationList.First().CurrencyPair);
            Assert.AreEqual(0, bboMemoryImage.BBORepresentationList.First().BestAskVolume);
            Assert.AreEqual(0, bboMemoryImage.BBORepresentationList.First().BestAskPrice);
            Assert.AreEqual(0, bboMemoryImage.BBORepresentationList.First().BestAskOrderCount);

            OutputDisruptor.ShutDown();
        }

        [Test]
        [Category(Integration)]
        public void AddSellBBoTest_ChecksIfAddingnewSellUpdatesTheBBOAtMemoryImageSide_VerifiesThroughMemoryImagesBBOList()
        {
            BBOMemoryImage bboMemoryImage = new BBOMemoryImage();

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
            Order sellOrder1 = OrderFactory.CreateOrder("1244", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 250, 496.34M, new StubbedOrderIdGenerator());
            Order sellOrder2 = OrderFactory.CreateOrder("1222", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 200, 495.34M, new StubbedOrderIdGenerator());
            Order sellOrder3 = OrderFactory.CreateOrder("1264", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 200, 492.34M, new StubbedOrderIdGenerator());
            Order sellOrder4 = OrderFactory.CreateOrder("12387", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 200, 492.34M, new StubbedOrderIdGenerator());
            Order sellOrder5 = OrderFactory.CreateOrder("123897", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 200, 493.34M, new StubbedOrderIdGenerator());

            exchange.PlaceNewOrder(sellOrder1);

            // Takes some time for the disruptor to broadcast changes to the memory image
            ManualResetEvent manualResetEvent = new ManualResetEvent(false);
            manualResetEvent.WaitOne(2000);

            Assert.AreEqual(0, exchange.ExchangeEssentials.First().LimitOrderBook.Bids.Count());
            Assert.AreEqual(1, exchange.ExchangeEssentials.First().LimitOrderBook.Asks.Count());

            // Number of currencies in the memory image that contain depth
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.Count());

            // Best bid and its depth stats
            Assert.AreEqual(CurrencyConstants.BtcUsd, bboMemoryImage.BBORepresentationList.First().CurrencyPair);
            Assert.AreEqual(0, bboMemoryImage.BBORepresentationList.First().BestBidVolume);
            Assert.AreEqual(0, bboMemoryImage.BBORepresentationList.First().BestBidPrice);
            Assert.AreEqual(0, bboMemoryImage.BBORepresentationList.First().BestBidOrderCount);

            // Best ask and its depth stats
            Assert.AreEqual(CurrencyConstants.BtcUsd, bboMemoryImage.BBORepresentationList.First().CurrencyPair);
            Assert.AreEqual(250, bboMemoryImage.BBORepresentationList.First().BestAskVolume);
            Assert.AreEqual(496.34M, bboMemoryImage.BBORepresentationList.First().BestAskPrice);
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.First().BestAskOrderCount);

            exchange.PlaceNewOrder(sellOrder2);

            // Takes some time for the disruptor to broadcast changes to the memory image
            manualResetEvent.Reset();
            manualResetEvent.WaitOne(2000);

            Assert.AreEqual(0, exchange.ExchangeEssentials.First().LimitOrderBook.Bids.Count());
            Assert.AreEqual(2, exchange.ExchangeEssentials.First().LimitOrderBook.Asks.Count());

            // Number of currencies in the memory image that contain depth
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.Count());

            // Best bid and its depth stats
            Assert.AreEqual(CurrencyConstants.BtcUsd, bboMemoryImage.BBORepresentationList.First().CurrencyPair);
            Assert.AreEqual(0, bboMemoryImage.BBORepresentationList.First().BestBidVolume);
            Assert.AreEqual(0, bboMemoryImage.BBORepresentationList.First().BestBidPrice);
            Assert.AreEqual(0, bboMemoryImage.BBORepresentationList.First().BestBidOrderCount);

            // Best ask and its depth stats
            Assert.AreEqual(CurrencyConstants.BtcUsd, bboMemoryImage.BBORepresentationList.First().CurrencyPair);
            Assert.AreEqual(200, bboMemoryImage.BBORepresentationList.First().BestAskVolume);
            Assert.AreEqual(495.34M, bboMemoryImage.BBORepresentationList.First().BestAskPrice);
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.First().BestAskOrderCount);

            exchange.PlaceNewOrder(sellOrder3);

            // Takes some time for the disruptor to broadcast changes to the memory image
            manualResetEvent.Reset();
            manualResetEvent.WaitOne(2000);

            Assert.AreEqual(0, exchange.ExchangeEssentials.First().LimitOrderBook.Bids.Count());
            Assert.AreEqual(3, exchange.ExchangeEssentials.First().LimitOrderBook.Asks.Count());

            // Number of currencies in the memory image that contain depth
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.Count());

            // Best bid and its depth stats
            Assert.AreEqual(CurrencyConstants.BtcUsd, bboMemoryImage.BBORepresentationList.First().CurrencyPair);
            Assert.AreEqual(0, bboMemoryImage.BBORepresentationList.First().BestBidVolume);
            Assert.AreEqual(0, bboMemoryImage.BBORepresentationList.First().BestBidPrice);
            Assert.AreEqual(0, bboMemoryImage.BBORepresentationList.First().BestBidOrderCount);

            // Best ask and its depth stats
            Assert.AreEqual(CurrencyConstants.BtcUsd, bboMemoryImage.BBORepresentationList.First().CurrencyPair);
            Assert.AreEqual(200, bboMemoryImage.BBORepresentationList.First().BestAskVolume);
            Assert.AreEqual(492.34M, bboMemoryImage.BBORepresentationList.First().BestAskPrice);
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.First().BestAskOrderCount);

            exchange.PlaceNewOrder(sellOrder4);

            // Takes some time for the disruptor to broadcast changes to the memory image
            manualResetEvent.Reset();
            manualResetEvent.WaitOne(2000);

            Assert.AreEqual(0, exchange.ExchangeEssentials.First().LimitOrderBook.Bids.Count());
            Assert.AreEqual(4, exchange.ExchangeEssentials.First().LimitOrderBook.Asks.Count());

            // Number of currencies in the memory image that contain depth
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.Count());

            // Best bid and its depth stats
            Assert.AreEqual(CurrencyConstants.BtcUsd, bboMemoryImage.BBORepresentationList.First().CurrencyPair);
            Assert.AreEqual(0, bboMemoryImage.BBORepresentationList.First().BestBidVolume);
            Assert.AreEqual(0, bboMemoryImage.BBORepresentationList.First().BestBidPrice);
            Assert.AreEqual(0, bboMemoryImage.BBORepresentationList.First().BestBidOrderCount);

            // Best ask and its depth stats
            // Two orders at the same price will accumulate the volume
            Assert.AreEqual(CurrencyConstants.BtcUsd, bboMemoryImage.BBORepresentationList.First().CurrencyPair);
            Assert.AreEqual(400, bboMemoryImage.BBORepresentationList.First().BestAskVolume);
            Assert.AreEqual(492.34M, bboMemoryImage.BBORepresentationList.First().BestAskPrice);
            Assert.AreEqual(2, bboMemoryImage.BBORepresentationList.First().BestAskOrderCount);

            exchange.PlaceNewOrder(sellOrder5);

            // Takes some time for the disruptor to broadcast changes to the memory image
            manualResetEvent.Reset();
            manualResetEvent.WaitOne(2000);

            Assert.AreEqual(0, exchange.ExchangeEssentials.First().LimitOrderBook.Bids.Count());
            Assert.AreEqual(5, exchange.ExchangeEssentials.First().LimitOrderBook.Asks.Count());

            // Number of currencies in the memory image that contain depth
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.Count());

            // Best bid and its depth stats
            Assert.AreEqual(CurrencyConstants.BtcUsd, bboMemoryImage.BBORepresentationList.First().CurrencyPair);
            Assert.AreEqual(0, bboMemoryImage.BBORepresentationList.First().BestBidVolume);
            Assert.AreEqual(0, bboMemoryImage.BBORepresentationList.First().BestBidPrice);
            Assert.AreEqual(0, bboMemoryImage.BBORepresentationList.First().BestBidOrderCount);

            // Best ask and its depth stats
            // No change in best ask as the new ask price is higher than the previous one
            Assert.AreEqual(CurrencyConstants.BtcUsd, bboMemoryImage.BBORepresentationList.First().CurrencyPair);
            Assert.AreEqual(400, bboMemoryImage.BBORepresentationList.First().BestAskVolume);
            Assert.AreEqual(492.34M, bboMemoryImage.BBORepresentationList.First().BestAskPrice);
            Assert.AreEqual(2, bboMemoryImage.BBORepresentationList.First().BestAskOrderCount);

            OutputDisruptor.ShutDown();
        }

        [Test]
        [Category(Integration)]
        public void AddBuysAndSellWIthoutMatchTest_VerifiesBBOWhenUnMatchingBuysAndSellsAreSent_VerifiesTHroughMemoryImagesLists()
        {
            BBOMemoryImage bboMemoryImage = new BBOMemoryImage();

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
                Constants.ORDER_SIDE_BUY, 100, 494.34M, new StubbedOrderIdGenerator());
            Order buyOrder3 = OrderFactory.CreateOrder("123498", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 100, 492.34M, new StubbedOrderIdGenerator());
            Order buyOrder4 = OrderFactory.CreateOrder("12355", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 100, 491.34M, new StubbedOrderIdGenerator());
            Order buyOrder5 = OrderFactory.CreateOrder("12356", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 500, 494.34M, new StubbedOrderIdGenerator());

            Order sellOrder1 = OrderFactory.CreateOrder("1244", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 250, 496.34M, new StubbedOrderIdGenerator());
            Order sellOrder2 = OrderFactory.CreateOrder("1222", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 200, 495.34M, new StubbedOrderIdGenerator());
            Order sellOrder3 = OrderFactory.CreateOrder("1264", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 200, 495.34M, new StubbedOrderIdGenerator());
            Order sellOrder4 = OrderFactory.CreateOrder("12387", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 200, 497.34M, new StubbedOrderIdGenerator());
            Order sellOrder5 = OrderFactory.CreateOrder("123897", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 100, 495.34M, new StubbedOrderIdGenerator());

            exchange.PlaceNewOrder(buyOrder1);
            exchange.PlaceNewOrder(sellOrder1);
            exchange.PlaceNewOrder(buyOrder2);
            exchange.PlaceNewOrder(sellOrder2);
            exchange.PlaceNewOrder(buyOrder3);
            exchange.PlaceNewOrder(sellOrder3);
            exchange.PlaceNewOrder(sellOrder4);
            exchange.PlaceNewOrder(buyOrder4);
            exchange.PlaceNewOrder(buyOrder5);            
            exchange.PlaceNewOrder(sellOrder5);

            // Takes some time for the disruptor to broadcast changes to the memory image
            ManualResetEvent manualResetEvent = new ManualResetEvent(false);
            manualResetEvent.WaitOne(2000);

            Assert.AreEqual(5, exchange.ExchangeEssentials.First().LimitOrderBook.Bids.Count());
            Assert.AreEqual(5, exchange.ExchangeEssentials.First().LimitOrderBook.Asks.Count());

            // Number of currencies in the memory image that contain depth
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.Count());

            // Best bid and its depth stats
            Assert.AreEqual(CurrencyConstants.BtcUsd, bboMemoryImage.BBORepresentationList.First().CurrencyPair);
            Assert.AreEqual(600, bboMemoryImage.BBORepresentationList.First().BestBidVolume);
            Assert.AreEqual(494.34M, bboMemoryImage.BBORepresentationList.First().BestBidPrice);
            Assert.AreEqual(2, bboMemoryImage.BBORepresentationList.First().BestBidOrderCount);

            // Best ask and its depth stats
            Assert.AreEqual(CurrencyConstants.BtcUsd, bboMemoryImage.BBORepresentationList.First().CurrencyPair);
            Assert.AreEqual(500, bboMemoryImage.BBORepresentationList.First().BestAskVolume);
            Assert.AreEqual(495.34M, bboMemoryImage.BBORepresentationList.First().BestAskPrice);
            Assert.AreEqual(3, bboMemoryImage.BBORepresentationList.First().BestAskOrderCount);

            OutputDisruptor.ShutDown();
        }

        [Test]
        [Category(Integration)]
        public void MatchBuyOrdersAndRemoveAsksFromBBO_ChecksWhtherAskBbosGetChangedAsExpected_VerifiesMemoryImageListsToVerify()
        {
            BBOMemoryImage bboMemoryImage = new BBOMemoryImage();

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
                Constants.ORDER_SIDE_BUY, 500, 495.34M, new StubbedOrderIdGenerator());
            Order buyOrder3 = OrderFactory.CreateOrder("123498", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 250, 496.34M, new StubbedOrderIdGenerator());
            Order buyOrder4 = OrderFactory.CreateOrder("12355", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 100, 491.34M, new StubbedOrderIdGenerator());
            Order buyOrder5 = OrderFactory.CreateOrder("12356", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 500, 494.34M, new StubbedOrderIdGenerator());

            Order sellOrder1 = OrderFactory.CreateOrder("1244", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 250, 496.34M, new StubbedOrderIdGenerator());
            Order sellOrder2 = OrderFactory.CreateOrder("1222", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 200, 495.34M, new StubbedOrderIdGenerator());
            Order sellOrder3 = OrderFactory.CreateOrder("1264", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 200, 495.34M, new StubbedOrderIdGenerator());
            Order sellOrder4 = OrderFactory.CreateOrder("12387", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 200, 497.34M, new StubbedOrderIdGenerator());
            Order sellOrder5 = OrderFactory.CreateOrder("123897", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 100, 495.34M, new StubbedOrderIdGenerator());

            exchange.PlaceNewOrder(buyOrder1);
            exchange.PlaceNewOrder(sellOrder1);
            exchange.PlaceNewOrder(sellOrder2);
            exchange.PlaceNewOrder(sellOrder3);
            exchange.PlaceNewOrder(sellOrder4);
            exchange.PlaceNewOrder(sellOrder5);

            // Takes some time for the disruptor to broadcast changes to the memory image
            ManualResetEvent manualResetEvent = new ManualResetEvent(false);
            manualResetEvent.WaitOne(2000);

            Assert.AreEqual(1, exchange.ExchangeEssentials.First().LimitOrderBook.Bids.Count());
            Assert.AreEqual(5, exchange.ExchangeEssentials.First().LimitOrderBook.Asks.Count());

            // Number of currencies in the memory image that contain depth
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.Count());

            // Best bid and its depth stats
            Assert.AreEqual(CurrencyConstants.BtcUsd, bboMemoryImage.BBORepresentationList.First().CurrencyPair);
            Assert.AreEqual(250, bboMemoryImage.BBORepresentationList.First().BestBidVolume);
            Assert.AreEqual(493.34M, bboMemoryImage.BBORepresentationList.First().BestBidPrice);
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.First().BestBidOrderCount);

            // Best ask and its depth stats
            Assert.AreEqual(CurrencyConstants.BtcUsd, bboMemoryImage.BBORepresentationList.First().CurrencyPair);
            Assert.AreEqual(500, bboMemoryImage.BBORepresentationList.First().BestAskVolume);
            Assert.AreEqual(495.34M, bboMemoryImage.BBORepresentationList.First().BestAskPrice);
            Assert.AreEqual(3, bboMemoryImage.BBORepresentationList.First().BestAskOrderCount);

            exchange.PlaceNewOrder(buyOrder2);

            manualResetEvent.Reset();
            manualResetEvent.WaitOne(2000);

            Assert.AreEqual(1, exchange.ExchangeEssentials.First().LimitOrderBook.Bids.Count());
            Assert.AreEqual(2, exchange.ExchangeEssentials.First().LimitOrderBook.Asks.Count());

            // Number of currencies in the memory image that contain depth
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.Count());

            // Best bid and its depth stats
            Assert.AreEqual(CurrencyConstants.BtcUsd, bboMemoryImage.BBORepresentationList.First().CurrencyPair);
            Assert.AreEqual(250, bboMemoryImage.BBORepresentationList.First().BestBidVolume);
            Assert.AreEqual(493.34M, bboMemoryImage.BBORepresentationList.First().BestBidPrice);
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.First().BestBidOrderCount);

            // Best ask and its depth stats
            Assert.AreEqual(CurrencyConstants.BtcUsd, bboMemoryImage.BBORepresentationList.First().CurrencyPair);
            Assert.AreEqual(250, bboMemoryImage.BBORepresentationList.First().BestAskVolume);
            Assert.AreEqual(496.34M, bboMemoryImage.BBORepresentationList.First().BestAskPrice);
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.First().BestAskOrderCount);

            exchange.PlaceNewOrder(buyOrder3);

            manualResetEvent.Reset();
            manualResetEvent.WaitOne(2000);

            Assert.AreEqual(1, exchange.ExchangeEssentials.First().LimitOrderBook.Bids.Count());
            Assert.AreEqual(1, exchange.ExchangeEssentials.First().LimitOrderBook.Asks.Count());

            // Number of currencies in the memory image that contain depth
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.Count());

            // Best bid and its depth stats
            Assert.AreEqual(CurrencyConstants.BtcUsd, bboMemoryImage.BBORepresentationList.First().CurrencyPair);
            Assert.AreEqual(250, bboMemoryImage.BBORepresentationList.First().BestBidVolume);
            Assert.AreEqual(493.34M, bboMemoryImage.BBORepresentationList.First().BestBidPrice);
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.First().BestBidOrderCount);

            // Best ask and its depth stats
            Assert.AreEqual(CurrencyConstants.BtcUsd, bboMemoryImage.BBORepresentationList.First().CurrencyPair);
            Assert.AreEqual(200, bboMemoryImage.BBORepresentationList.First().BestAskVolume);
            Assert.AreEqual(497.34M, bboMemoryImage.BBORepresentationList.First().BestAskPrice);
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.First().BestAskOrderCount);

            OutputDisruptor.ShutDown();
        }

        [Test]
        [Category(Integration)]
        public void MatchSellOrdersAndRemoveBidsFromBBO_ChecksWhtherBidBbosGetChangedAsExpected_VerifiesMemoryImageListsToVerify()
        {
            BBOMemoryImage bboMemoryImage = new BBOMemoryImage();

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
                Constants.ORDER_SIDE_BUY, 500, 495.34M, new StubbedOrderIdGenerator());
            Order buyOrder3 = OrderFactory.CreateOrder("123498", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 250, 496.34M, new StubbedOrderIdGenerator());
            Order buyOrder4 = OrderFactory.CreateOrder("12355", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 100, 491.34M, new StubbedOrderIdGenerator());
            Order buyOrder5 = OrderFactory.CreateOrder("12356", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 500, 494.34M, new StubbedOrderIdGenerator());

            Order sellOrder1 = OrderFactory.CreateOrder("1244", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 250, 497.34M, new StubbedOrderIdGenerator());
            Order sellOrder2 = OrderFactory.CreateOrder("1222", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 250, 496.34M, new StubbedOrderIdGenerator());
            Order sellOrder3 = OrderFactory.CreateOrder("1264", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 200, 495.34M, new StubbedOrderIdGenerator());
            Order sellOrder4 = OrderFactory.CreateOrder("12387", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 300, 495.34M, new StubbedOrderIdGenerator());
            Order sellOrder5 = OrderFactory.CreateOrder("123897", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 100, 495.34M, new StubbedOrderIdGenerator());

            exchange.PlaceNewOrder(sellOrder1);
            exchange.PlaceNewOrder(buyOrder1);
            exchange.PlaceNewOrder(buyOrder2);
            exchange.PlaceNewOrder(buyOrder3);
            exchange.PlaceNewOrder(buyOrder4);
            exchange.PlaceNewOrder(buyOrder5);

            // Takes some time for the disruptor to broadcast changes to the memory image
            ManualResetEvent manualResetEvent = new ManualResetEvent(false);
            manualResetEvent.WaitOne(2000);

            Assert.AreEqual(5, exchange.ExchangeEssentials.First().LimitOrderBook.Bids.Count());
            Assert.AreEqual(1, exchange.ExchangeEssentials.First().LimitOrderBook.Asks.Count());

            // Number of currencies in the memory image that contain depth
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.Count());

            // Best bid and its depth stats
            Assert.AreEqual(CurrencyConstants.BtcUsd, bboMemoryImage.BBORepresentationList.First().CurrencyPair);
            Assert.AreEqual(250, bboMemoryImage.BBORepresentationList.First().BestBidVolume);
            Assert.AreEqual(496.34M, bboMemoryImage.BBORepresentationList.First().BestBidPrice);
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.First().BestBidOrderCount);

            // Best ask and its depth stats
            Assert.AreEqual(CurrencyConstants.BtcUsd, bboMemoryImage.BBORepresentationList.First().CurrencyPair);
            Assert.AreEqual(250, bboMemoryImage.BBORepresentationList.First().BestAskVolume);
            Assert.AreEqual(497.34M, bboMemoryImage.BBORepresentationList.First().BestAskPrice);
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.First().BestAskOrderCount);

            exchange.PlaceNewOrder(sellOrder2);

            // Takes some time for the disruptor to broadcast changes to the memory image
            manualResetEvent.Reset();
            manualResetEvent.WaitOne(2000);

            Assert.AreEqual(4, exchange.ExchangeEssentials.First().LimitOrderBook.Bids.Count());
            Assert.AreEqual(1, exchange.ExchangeEssentials.First().LimitOrderBook.Asks.Count());

            // Number of currencies in the memory image that contain depth
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.Count());

            // Best bid and its depth stats
            Assert.AreEqual(CurrencyConstants.BtcUsd, bboMemoryImage.BBORepresentationList.First().CurrencyPair);
            Assert.AreEqual(500, bboMemoryImage.BBORepresentationList.First().BestBidVolume);
            Assert.AreEqual(495.34M, bboMemoryImage.BBORepresentationList.First().BestBidPrice);
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.First().BestBidOrderCount);

            // Best ask and its depth stats
            Assert.AreEqual(250, bboMemoryImage.BBORepresentationList.First().BestAskVolume);
            Assert.AreEqual(497.34M, bboMemoryImage.BBORepresentationList.First().BestAskPrice);
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.First().BestAskOrderCount);

            exchange.PlaceNewOrder(sellOrder3);

            // Takes some time for the disruptor to broadcast changes to the memory image
            manualResetEvent.Reset();
            manualResetEvent.WaitOne(2000);

            Assert.AreEqual(4, exchange.ExchangeEssentials.First().LimitOrderBook.Bids.Count());
            Assert.AreEqual(1, exchange.ExchangeEssentials.First().LimitOrderBook.Asks.Count());

            // Number of currencies in the memory image that contain depth
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.Count());

            // Best bid and its depth stats
            Assert.AreEqual(CurrencyConstants.BtcUsd, bboMemoryImage.BBORepresentationList.First().CurrencyPair);
            Assert.AreEqual(300, bboMemoryImage.BBORepresentationList.First().BestBidVolume);
            Assert.AreEqual(495.34M, bboMemoryImage.BBORepresentationList.First().BestBidPrice);
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.First().BestBidOrderCount);

            // Best ask and its depth stats
            Assert.AreEqual(250, bboMemoryImage.BBORepresentationList.First().BestAskVolume);
            Assert.AreEqual(497.34M, bboMemoryImage.BBORepresentationList.First().BestAskPrice);
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.First().BestAskOrderCount);

            exchange.PlaceNewOrder(sellOrder4);

            // Takes some time for the disruptor to broadcast changes to the memory image
            manualResetEvent.Reset();
            manualResetEvent.WaitOne(2000);

            Assert.AreEqual(3, exchange.ExchangeEssentials.First().LimitOrderBook.Bids.Count());
            Assert.AreEqual(1, exchange.ExchangeEssentials.First().LimitOrderBook.Asks.Count());

            // Number of currencies in the memory image that contain depth
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.Count());

            // Best bid and its depth stats
            Assert.AreEqual(CurrencyConstants.BtcUsd, bboMemoryImage.BBORepresentationList.First().CurrencyPair);
            Assert.AreEqual(500, bboMemoryImage.BBORepresentationList.First().BestBidVolume);
            Assert.AreEqual(494.34M, bboMemoryImage.BBORepresentationList.First().BestBidPrice);
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.First().BestBidOrderCount);

            // Best ask and its depth stats
            Assert.AreEqual(250, bboMemoryImage.BBORepresentationList.First().BestAskVolume);
            Assert.AreEqual(497.34M, bboMemoryImage.BBORepresentationList.First().BestAskPrice);
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.First().BestAskOrderCount);

            exchange.PlaceNewOrder(sellOrder5);

            // Takes some time for the disruptor to broadcast changes to the memory image
            manualResetEvent.Reset();
            manualResetEvent.WaitOne(2000);

            Assert.AreEqual(3, exchange.ExchangeEssentials.First().LimitOrderBook.Bids.Count());
            Assert.AreEqual(2, exchange.ExchangeEssentials.First().LimitOrderBook.Asks.Count());

            // Number of currencies in the memory image that contain depth
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.Count());

            // Best bid and its depth stats
            Assert.AreEqual(CurrencyConstants.BtcUsd, bboMemoryImage.BBORepresentationList.First().CurrencyPair);
            Assert.AreEqual(500, bboMemoryImage.BBORepresentationList.First().BestBidVolume);
            Assert.AreEqual(494.34M, bboMemoryImage.BBORepresentationList.First().BestBidPrice);
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.First().BestBidOrderCount);

            // Best ask and its depth stats
            Assert.AreEqual(100, bboMemoryImage.BBORepresentationList.First().BestAskVolume);
            Assert.AreEqual(495.34M, bboMemoryImage.BBORepresentationList.First().BestAskPrice);
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.First().BestAskOrderCount);

            OutputDisruptor.ShutDown();
        }

        [Test]
        [Category(Integration)]
        public void CancelBidsAndAsks_VerifyAfterCancellingThatTheBboIsUpdated_VerifiesUsingMemoryImagesLists()
        {
            BBOMemoryImage bboMemoryImage = new BBOMemoryImage();

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
                Constants.ORDER_SIDE_BUY, 250, 483.34M, new StubbedOrderIdGenerator());
            Order buyOrder2 = OrderFactory.CreateOrder("1234", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 500, 485.34M, new StubbedOrderIdGenerator());
            Order buyOrder3 = OrderFactory.CreateOrder("123498", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 250, 486.34M, new StubbedOrderIdGenerator());
            Order buyOrder4 = OrderFactory.CreateOrder("12355", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 100, 481.34M, new StubbedOrderIdGenerator());
            Order buyOrder5 = OrderFactory.CreateOrder("12356", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_BUY, 500, 484.34M, new StubbedOrderIdGenerator());

            Order sellOrder1 = OrderFactory.CreateOrder("1244", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 250, 497.34M, new StubbedOrderIdGenerator());
            Order sellOrder2 = OrderFactory.CreateOrder("1222", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 250, 496.34M, new StubbedOrderIdGenerator());
            Order sellOrder3 = OrderFactory.CreateOrder("1264", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 200, 495.34M, new StubbedOrderIdGenerator());
            Order sellOrder4 = OrderFactory.CreateOrder("12387", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 300, 495.34M, new StubbedOrderIdGenerator());
            Order sellOrder5 = OrderFactory.CreateOrder("123897", CurrencyConstants.BtcUsd, Constants.ORDER_TYPE_LIMIT,
                Constants.ORDER_SIDE_SELL, 100, 492.34M, new StubbedOrderIdGenerator());

            exchange.PlaceNewOrder(sellOrder1);
            exchange.PlaceNewOrder(sellOrder2);
            exchange.PlaceNewOrder(sellOrder3);
            exchange.PlaceNewOrder(sellOrder4);
            exchange.PlaceNewOrder(sellOrder5);
            exchange.PlaceNewOrder(buyOrder1);
            exchange.PlaceNewOrder(buyOrder2);
            exchange.PlaceNewOrder(buyOrder3);
            exchange.PlaceNewOrder(buyOrder4);
            exchange.PlaceNewOrder(buyOrder5);

            // Cancels in the following order:
            OrderId buyOrder3Id = buyOrder3.OrderId; // Best Bid should change
            OrderId buyOrder4Id = buyOrder4.OrderId; // Best Bid should not change
            OrderId sellOrder1Id = sellOrder1.OrderId; // Best Ask should not change
            OrderId buyOrder2Id = buyOrder2.OrderId; // Best Bid should change
            OrderId buyOrder5Id = buyOrder5.OrderId; // Best Bid should change
            OrderId sellOrder5Id = sellOrder5.OrderId; // Best Ask should change
            OrderId sellOrder3Id = sellOrder3.OrderId;  // Best Ask's volume only should change

            // Takes some time for the disruptor to broadcast changes to the memory image
            ManualResetEvent manualResetEvent = new ManualResetEvent(false);
            manualResetEvent.WaitOne(2000);

            Assert.AreEqual(5, exchange.ExchangeEssentials.First().LimitOrderBook.Bids.Count());
            Assert.AreEqual(5, exchange.ExchangeEssentials.First().LimitOrderBook.Asks.Count());

            // Number of currencies in the memory image that contain depth
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.Count());

            // Best bid and its depth stats
            Assert.AreEqual(CurrencyConstants.BtcUsd, bboMemoryImage.BBORepresentationList.First().CurrencyPair);
            Assert.AreEqual(250, bboMemoryImage.BBORepresentationList.First().BestBidVolume);
            Assert.AreEqual(486.34M, bboMemoryImage.BBORepresentationList.First().BestBidPrice);
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.First().BestBidOrderCount);

            // Best ask and its depth stats
            Assert.AreEqual(CurrencyConstants.BtcUsd, bboMemoryImage.BBORepresentationList.First().CurrencyPair);
            Assert.AreEqual(100, bboMemoryImage.BBORepresentationList.First().BestAskVolume);
            Assert.AreEqual(492.34M, bboMemoryImage.BBORepresentationList.First().BestAskPrice);
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.First().BestAskOrderCount);

            // Best Bid Should change
            exchange.CancelOrder(new OrderCancellation(buyOrder3Id, buyOrder3.TraderId, buyOrder3.CurrencyPair));
            manualResetEvent.Reset();
            manualResetEvent.WaitOne(2000);

            Assert.AreEqual(4, exchange.ExchangeEssentials.First().LimitOrderBook.Bids.Count());
            Assert.AreEqual(5, exchange.ExchangeEssentials.First().LimitOrderBook.Asks.Count());

            // Number of currencies in the memory image that contain depth
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.Count());

            // Best bid and its depth stats
            Assert.AreEqual(CurrencyConstants.BtcUsd, bboMemoryImage.BBORepresentationList.First().CurrencyPair);
            Assert.AreEqual(500, bboMemoryImage.BBORepresentationList.First().BestBidVolume);
            Assert.AreEqual(485.34M, bboMemoryImage.BBORepresentationList.First().BestBidPrice);
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.First().BestBidOrderCount);

            // Best ask and its depth stats
            Assert.AreEqual(CurrencyConstants.BtcUsd, bboMemoryImage.BBORepresentationList.First().CurrencyPair);
            Assert.AreEqual(100, bboMemoryImage.BBORepresentationList.First().BestAskVolume);
            Assert.AreEqual(492.34M, bboMemoryImage.BBORepresentationList.First().BestAskPrice);
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.First().BestAskOrderCount);

            // Nothing should change by cancelling this order
            exchange.CancelOrder(new OrderCancellation(buyOrder4Id, buyOrder4.TraderId, buyOrder4.CurrencyPair));
            manualResetEvent.Reset();
            manualResetEvent.WaitOne(2000);
            // Order cancelled
            Assert.AreEqual(3, exchange.ExchangeEssentials.First().LimitOrderBook.Bids.Count());
            Assert.AreEqual(5, exchange.ExchangeEssentials.First().LimitOrderBook.Asks.Count());

            // Number of currencies in the memory image that contain depth
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.Count());

            // Best bid and its depth stats
            Assert.AreEqual(CurrencyConstants.BtcUsd, bboMemoryImage.BBORepresentationList.First().CurrencyPair);
            Assert.AreEqual(500, bboMemoryImage.BBORepresentationList.First().BestBidVolume);
            Assert.AreEqual(485.34M, bboMemoryImage.BBORepresentationList.First().BestBidPrice);
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.First().BestBidOrderCount);

            // Best ask and its depth stats
            Assert.AreEqual(CurrencyConstants.BtcUsd, bboMemoryImage.BBORepresentationList.First().CurrencyPair);
            Assert.AreEqual(100, bboMemoryImage.BBORepresentationList.First().BestAskVolume);
            Assert.AreEqual(492.34M, bboMemoryImage.BBORepresentationList.First().BestAskPrice);
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.First().BestAskOrderCount);

            // Best ask should not change by cancelling this order 
            exchange.CancelOrder(new OrderCancellation(sellOrder1Id, sellOrder1.TraderId, sellOrder1.CurrencyPair));
            manualResetEvent.Reset();
            manualResetEvent.WaitOne(2000);
            
            Assert.AreEqual(3, exchange.ExchangeEssentials.First().LimitOrderBook.Bids.Count());
            // Order cancelled
            Assert.AreEqual(4, exchange.ExchangeEssentials.First().LimitOrderBook.Asks.Count());

            // Number of currencies in the memory image that contain depth
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.Count());

            // Best bid and its depth stats
            Assert.AreEqual(CurrencyConstants.BtcUsd, bboMemoryImage.BBORepresentationList.First().CurrencyPair);
            Assert.AreEqual(500, bboMemoryImage.BBORepresentationList.First().BestBidVolume);
            Assert.AreEqual(485.34M, bboMemoryImage.BBORepresentationList.First().BestBidPrice);
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.First().BestBidOrderCount);

            // Best ask and its depth stats
            Assert.AreEqual(CurrencyConstants.BtcUsd, bboMemoryImage.BBORepresentationList.First().CurrencyPair);
            Assert.AreEqual(100, bboMemoryImage.BBORepresentationList.First().BestAskVolume);
            Assert.AreEqual(492.34M, bboMemoryImage.BBORepresentationList.First().BestAskPrice);
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.First().BestAskOrderCount);
            // Best bid must change by cancelling this order
            exchange.CancelOrder(new OrderCancellation(buyOrder2Id, buyOrder2.TraderId, buyOrder2.CurrencyPair));
            manualResetEvent.Reset();
            manualResetEvent.WaitOne(2000);

            Assert.AreEqual(2, exchange.ExchangeEssentials.First().LimitOrderBook.Bids.Count());
            // Order cancelled
            Assert.AreEqual(4, exchange.ExchangeEssentials.First().LimitOrderBook.Asks.Count());

            // Number of currencies in the memory image that contain depth
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.Count());

            // Best bid and its depth stats
            Assert.AreEqual(CurrencyConstants.BtcUsd, bboMemoryImage.BBORepresentationList.First().CurrencyPair);
            Assert.AreEqual(500, bboMemoryImage.BBORepresentationList.First().BestBidVolume);
            Assert.AreEqual(484.34M, bboMemoryImage.BBORepresentationList.First().BestBidPrice);
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.First().BestBidOrderCount);

            // Best ask and its depth stats
            Assert.AreEqual(CurrencyConstants.BtcUsd, bboMemoryImage.BBORepresentationList.First().CurrencyPair);
            Assert.AreEqual(100, bboMemoryImage.BBORepresentationList.First().BestAskVolume);
            Assert.AreEqual(492.34M, bboMemoryImage.BBORepresentationList.First().BestAskPrice);
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.First().BestAskOrderCount);

            // Best bid must change by cancelling this order
            exchange.CancelOrder(new OrderCancellation(buyOrder5Id, buyOrder5.TraderId, buyOrder5.CurrencyPair));
            manualResetEvent.Reset();
            manualResetEvent.WaitOne(2000);

            Assert.AreEqual(1, exchange.ExchangeEssentials.First().LimitOrderBook.Bids.Count());
            // Order cancelled
            Assert.AreEqual(4, exchange.ExchangeEssentials.First().LimitOrderBook.Asks.Count());

            // Number of currencies in the memory image that contain depth
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.Count());

            // Best bid and its depth stats
            Assert.AreEqual(CurrencyConstants.BtcUsd, bboMemoryImage.BBORepresentationList.First().CurrencyPair);
            Assert.AreEqual(250, bboMemoryImage.BBORepresentationList.First().BestBidVolume);
            Assert.AreEqual(483.34M, bboMemoryImage.BBORepresentationList.First().BestBidPrice);
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.First().BestBidOrderCount);

            // Best ask and its depth stats
            Assert.AreEqual(CurrencyConstants.BtcUsd, bboMemoryImage.BBORepresentationList.First().CurrencyPair);
            Assert.AreEqual(100, bboMemoryImage.BBORepresentationList.First().BestAskVolume);
            Assert.AreEqual(492.34M, bboMemoryImage.BBORepresentationList.First().BestAskPrice);
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.First().BestAskOrderCount);

            // Best Ask must change by cancelling this order
            exchange.CancelOrder(new OrderCancellation(sellOrder5Id, sellOrder5.TraderId, sellOrder5.CurrencyPair));
            manualResetEvent.Reset();
            manualResetEvent.WaitOne(2000);

            Assert.AreEqual(1, exchange.ExchangeEssentials.First().LimitOrderBook.Bids.Count());
            // Order cancelled
            Assert.AreEqual(3, exchange.ExchangeEssentials.First().LimitOrderBook.Asks.Count());

            // Number of currencies in the memory image that contain depth
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.Count());

            // Best bid and its depth stats
            Assert.AreEqual(CurrencyConstants.BtcUsd, bboMemoryImage.BBORepresentationList.First().CurrencyPair);
            Assert.AreEqual(250, bboMemoryImage.BBORepresentationList.First().BestBidVolume);
            Assert.AreEqual(483.34M, bboMemoryImage.BBORepresentationList.First().BestBidPrice);
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.First().BestBidOrderCount);

            // Best ask and its depth stats
            Assert.AreEqual(CurrencyConstants.BtcUsd, bboMemoryImage.BBORepresentationList.First().CurrencyPair);
            Assert.AreEqual(500, bboMemoryImage.BBORepresentationList.First().BestAskVolume);
            Assert.AreEqual(495.34M, bboMemoryImage.BBORepresentationList.First().BestAskPrice);
            Assert.AreEqual(2, bboMemoryImage.BBORepresentationList.First().BestAskOrderCount);

            // Best Ask must change by cancelling this order
            exchange.CancelOrder(new OrderCancellation(sellOrder3Id, sellOrder3.TraderId, sellOrder3.CurrencyPair));
            manualResetEvent.Reset();
            manualResetEvent.WaitOne(2000);

            Assert.AreEqual(1, exchange.ExchangeEssentials.First().LimitOrderBook.Bids.Count());
            // Order cancelled
            Assert.AreEqual(2, exchange.ExchangeEssentials.First().LimitOrderBook.Asks.Count());

            // Number of currencies in the memory image that contain depth
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.Count());

            // Best bid and its depth stats
            Assert.AreEqual(CurrencyConstants.BtcUsd, bboMemoryImage.BBORepresentationList.First().CurrencyPair);
            Assert.AreEqual(250, bboMemoryImage.BBORepresentationList.First().BestBidVolume);
            Assert.AreEqual(483.34M, bboMemoryImage.BBORepresentationList.First().BestBidPrice);
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.First().BestBidOrderCount);

            // Best ask and its depth stats
            Assert.AreEqual(CurrencyConstants.BtcUsd, bboMemoryImage.BBORepresentationList.First().CurrencyPair);
            Assert.AreEqual(300, bboMemoryImage.BBORepresentationList.First().BestAskVolume);
            Assert.AreEqual(495.34M, bboMemoryImage.BBORepresentationList.First().BestAskPrice);
            Assert.AreEqual(1, bboMemoryImage.BBORepresentationList.First().BestAskOrderCount);

            OutputDisruptor.ShutDown();
        }

        #endregion Disruptor Linkage Tests
    }
}
