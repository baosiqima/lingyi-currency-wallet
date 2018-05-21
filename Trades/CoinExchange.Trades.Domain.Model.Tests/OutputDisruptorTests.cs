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
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CoinExchange.Common.Utility;
using CoinExchange.Trades.Domain.Model.OrderAggregate;
using CoinExchange.Trades.Domain.Model.OrderMatchingEngine;
using CoinExchange.Trades.Domain.Model.Services;
using CoinExchange.Trades.Domain.Model.TradeAggregate;
using CoinExchange.Trades.Infrastructure.Services;
using Disruptor;
using NUnit.Framework;

namespace CoinExchange.Trades.Domain.Model.Tests
{
    public class OutputDisruptorTests:IEventHandler<byte[]>
    {
        private ManualResetEvent _manualResetEvent;
        private Order _receviedOrder;
        private Trade _receivedTrade;
        private LimitOrderBook _receivedLimitOrderBook;
        private Depth _receivedDepth;
        private BBO _receivedBbo;
        public void OnNext(byte[] data, long sequence, bool endOfBatch)
        {
            object getObject = StreamConversion.ByteArrayToObject(data);
            if (getObject is Order)
            {
                _receviedOrder = getObject as Order;
            }
            if (getObject is Trade)      
            {
                _receivedTrade = getObject as Trade;
            }
            if (getObject is LimitOrderBook)
            {
                _receivedLimitOrderBook = getObject as LimitOrderBook;
            }
            if (getObject is Depth)
            {
                _receivedDepth = getObject as Depth;
            }
            if (getObject is BBO)
            {
                _receivedBbo = getObject as BBO;
            }
        }

        [SetUp]
        public void Setup()
        {
            OutputDisruptor.InitializeDisruptor(new IEventHandler<byte[]>[]{this});
            _manualResetEvent = new ManualResetEvent(false);
        }

        [TearDown]
        public void TearDown()
        {
            OutputDisruptor.ShutDown();
        }

        [Test]
        public void PublishOrderToOutputDisruptor_IfOrderIsConvertedToByteArray_ItShouldBeReceivedAndCastedToOrder()
        {
            Order order = OrderFactory.CreateOrder("1234", "XBTUSD", "market", "buy", 5, 0,
               new StubbedOrderIdGenerator());
            //byte[] array = ObjectToByteArray(order);
            OutputDisruptor.Publish(order);
            _manualResetEvent.WaitOne(3000);
            Assert.NotNull(_receviedOrder);
            Assert.AreEqual(_receviedOrder, order);
        }

        [Test]
        public void PublishTradeToOutputDisruptor_IfTradeIsConvertedToByteArray_ItShouldBeReceivedAndCastedToTrade()
        {
            Order buyOrder = OrderFactory.CreateOrder("1234", "XBTUSD", "market", "buy", 5, 0,
               new StubbedOrderIdGenerator());
            Order sellOrder = OrderFactory.CreateOrder("1234", "XBTUSD", "market", "sell", 5, 0,
               new StubbedOrderIdGenerator());
            //Trade trade=new Trade("XBTUSD",new Price(100),new Volume(10),DateTime.Now,buyOrder,sellOrder);
            Trade trade = TradeFactory.GenerateTrade("XBTUSD", new Price(100), new Volume(10), buyOrder, sellOrder);
            //byte[] array = ObjectToByteArray(trade);
            OutputDisruptor.Publish(trade);
            _manualResetEvent.WaitOne(3000);
            Assert.NotNull(_receivedTrade);
            Assert.AreEqual(_receivedTrade.TradeId.Id,trade.TradeId.Id);
            Assert.AreEqual(_receivedTrade.BuyOrder, buyOrder);
            Assert.AreEqual(_receivedTrade.SellOrder, sellOrder);
        }

        [Test]
        public void PublishOrderBookToDisruptor_IfOrderBookIsConvertedToByteArray_ItShouldBeReceivedAndCastedToOrderBook()
        {
            LimitOrderBook limitOrderBook = new LimitOrderBook("XBTUSD");
            Order buyOrder = OrderFactory.CreateOrder("1234", "XBTUSD", "limit", "buy", 5, 10,
               new StubbedOrderIdGenerator());
            Order sellOrder = OrderFactory.CreateOrder("1234", "XBTUSD", "limit", "sell", 5, 11,
               new StubbedOrderIdGenerator());
            limitOrderBook.PlaceOrder(buyOrder);
            limitOrderBook.PlaceOrder(sellOrder);
            //byte[] array = ObjectToByteArray(limitOrderBook);
            OutputDisruptor.Publish(limitOrderBook);
            _manualResetEvent.WaitOne(3000);
            Assert.NotNull(_receivedLimitOrderBook);
            Assert.AreEqual(_receivedLimitOrderBook.AskCount,1);
            Assert.AreEqual(_receivedLimitOrderBook.BidCount, 1);
        }

        [Test]
        public void PublishDepthToDisruptor_IfDepthIsConvertedToByteArray_ItShouldBeReceivedAndCastedToDepth()
        {
            Depth depth = new Depth("XBTUSD", 3);
            depth.AddOrder(new Price(490), new Volume(100), OrderSide.Buy);
            depth.AddOrder(new Price(491), new Volume(100), OrderSide.Buy);
            depth.AddOrder(new Price(492), new Volume(200), OrderSide.Buy);
            //byte[] array = ObjectToByteArray(depth);
            OutputDisruptor.Publish(depth);
            _manualResetEvent.WaitOne(3000);
            Assert.NotNull(_receivedDepth);
            Assert.AreEqual(_receivedDepth.BidLevels[0].Price.Value,492);
            Assert.AreEqual(_receivedDepth.BidLevels[1].Price.Value, 491);
            Assert.AreEqual(_receivedDepth.BidLevels[2].Price.Value, 490);
        }

        [Test]
        public void PublishBboToDisruptor_IfBboIsConvertedToByteArray_ItShouldBeReceivedAndCastedToBbo()
        {
            DepthLevel askDepthLevel = new DepthLevel(new Price(491.32M));
            bool addOrder1 = askDepthLevel.AddOrder(new Volume(2000));
            bool addOrder2 = askDepthLevel.AddOrder(new Volume(1000));
            DepthLevel bidDepthLevel = new DepthLevel(new Price(491.32M));
            addOrder1 = bidDepthLevel.AddOrder(new Volume(2000));
            addOrder2 = bidDepthLevel.AddOrder(new Volume(1000));
            bool addOrder3 = bidDepthLevel.AddOrder(new Volume(3000));
            BBO bbo=new BBO("BTCUSD", bidDepthLevel, askDepthLevel);
            //byte[] array = ObjectToByteArray(bbo);
            OutputDisruptor.Publish(bbo);
            _manualResetEvent.WaitOne(3000);
            Assert.NotNull(_receivedBbo);
            Assert.AreEqual(_receivedBbo.BestAsk.OrderCount,2);
            Assert.AreEqual(_receivedBbo.BestBid.OrderCount,3);
        }

        [Test]
        public void PublishAllTypesToOutputDisruptor_IfAllTypesAreConvertedToByteArray_ItShouldReceivedAndProperlyCasted()
        {
            Order order = OrderFactory.CreateOrder("1234", "XBTUSD", "market", "buy", 5, 0,
               new StubbedOrderIdGenerator());
            //byte[] array = ObjectToByteArray(order);
            OutputDisruptor.Publish(order);

            Order buyOrder = OrderFactory.CreateOrder("1234", "XBTUSD", "limit", "buy", 5, 10,
              new StubbedOrderIdGenerator());
            Order sellOrder = OrderFactory.CreateOrder("1234", "XBTUSD", "limit", "sell", 5, 11,
               new StubbedOrderIdGenerator());
            Trade trade = new Trade(new TradeId("123"), "XBTUSD", new Price(100), new Volume(10), DateTime.Now, buyOrder, sellOrder);
            //byte[] array1 = ObjectToByteArray(trade);
            OutputDisruptor.Publish(trade);

            LimitOrderBook limitOrderBook = new LimitOrderBook("XBTUSD");
            limitOrderBook.PlaceOrder(buyOrder);
            limitOrderBook.PlaceOrder(sellOrder);
            //byte[] array2 = ObjectToByteArray(limitOrderBook);
            OutputDisruptor.Publish(limitOrderBook);

            Depth depth = new Depth("XBTUSD", 3);
            depth.AddOrder(new Price(490), new Volume(100), OrderSide.Buy);
            depth.AddOrder(new Price(491), new Volume(100), OrderSide.Buy);
            depth.AddOrder(new Price(492), new Volume(200), OrderSide.Buy);
            //byte[] array3 = ObjectToByteArray(depth);
            OutputDisruptor.Publish(depth);

            DepthLevel askDepthLevel = new DepthLevel(new Price(491.32M));
            bool addOrder1 = askDepthLevel.AddOrder(new Volume(2000));
            bool addOrder2 = askDepthLevel.AddOrder(new Volume(1000));
            DepthLevel bidDepthLevel = new DepthLevel(new Price(491.32M));
            addOrder1 = bidDepthLevel.AddOrder(new Volume(2000));
            addOrder2 = bidDepthLevel.AddOrder(new Volume(1000));
            bool addOrder3 = bidDepthLevel.AddOrder(new Volume(3000));
            BBO bbo = new BBO("XBTUSD", bidDepthLevel, askDepthLevel);
            //byte[] array4 = ObjectToByteArray(bbo);
            OutputDisruptor.Publish(bbo);
            _manualResetEvent.WaitOne(3000);
            
            Assert.NotNull(_receviedOrder);
            Assert.AreEqual(_receviedOrder, order);

            Assert.NotNull(_receivedTrade);
            Assert.AreEqual(_receivedTrade.BuyOrder, buyOrder);
            Assert.AreEqual(_receivedTrade.SellOrder, sellOrder);

            Assert.NotNull(_receivedLimitOrderBook);
            Assert.AreEqual(_receivedLimitOrderBook.AskCount, 1);
            Assert.AreEqual(_receivedLimitOrderBook.BidCount, 1);

            Assert.NotNull(_receivedDepth);
            Assert.AreEqual(_receivedDepth.BidLevels[0].Price.Value, 492);
            Assert.AreEqual(_receivedDepth.BidLevels[1].Price.Value, 491);
            Assert.AreEqual(_receivedDepth.BidLevels[2].Price.Value, 490);
            
            Assert.NotNull(_receivedBbo);
            Assert.AreEqual(_receivedBbo.BestAsk.OrderCount, 2);
            Assert.AreEqual(_receivedBbo.BestBid.OrderCount, 3);
        }

        private static byte[] ObjectToByteArray(Object obj)
        {
            if (obj == null)
                return null;
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            bf.Serialize(ms, obj);
            return ms.ToArray();
        }

        private static Object ByteArrayToObject(byte[] arrBytes)
        {
            MemoryStream memStream = new MemoryStream();
            BinaryFormatter binForm = new BinaryFormatter();
            memStream.Write(arrBytes, 0, arrBytes.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            Object obj = (Object)binForm.Deserialize(memStream);
            return obj;
        }
    }
}
