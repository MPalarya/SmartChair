using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Threading;
using System.Diagnostics;
using System.Management;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Common.Exceptions;

namespace RPiConsole
{
    public class RPiConsole
    {
        #region Fields

        internal static Sensor sensorBigLeft = new Sensor(ESensorType.FlexiForceA201, 0);
        internal static Sensor sensorBigRight = new Sensor(ESensorType.FlexiForceA201, 1);
        private static DeviceDataAggregator chair = DeviceDataAggregator.Instance;
        private static Timer readSensorTimer;
        private static ConsoleArgumentsHandler argsHandler = ConsoleArgumentsHandler.Instance;

        #endregion

        #region Main
        static void Main(string[] args)
        {
            setupSensors();
            setupTimer();
            readUserInput();
        }
        #endregion

        #region Methods
        private static void setupSensors()
        {
            sensorBigLeft.connectAdcDeviceAsync();
            sensorBigRight.connectAdcDeviceAsync();

            chair.Seat[EChairPartArea.LeftMid] = sensorBigLeft;
            chair.Seat[EChairPartArea.RightMid] = sensorBigRight;
        }

        private static void setupTimer()
        {
            readSensorTimer = new Timer(readAllSensorsTick, null, DeviceDataAggregator.frequencyToReport, DeviceDataAggregator.frequencyToReport);
        }

        private static void readAllSensorsTick(object state)
        {
            chair.aggregateAndReportData();
        }

        private static void readUserInput()
        {
            string line;
            while (true)
            {
                line = Console.ReadLine();
                argsHandler.parseLine(line);
            }
        }
        #endregion
    }

    public class ConsoleArgumentsHandler
    {
        #region Fields
        private static ConsoleArgumentsHandler instance;
        private ParseNode parseTree;
        #endregion

        #region Constructors
        private ConsoleArgumentsHandler()
        {
            createParseTree();
        }

        private void createParseTree()
        {
            parseTree = new ParseNode("", printUsage);

            ParseNode calibrateNode = new ParseNode("calibrate", printUsage);
            ParseNode calibrateAllNode = new ParseNode("all", calibrateAll);
            ParseNode calibrateBigLeftNode = new ParseNode("bigLeft", calibrateBigLeft);
            ParseNode calibrateBigRightNode = new ParseNode("bigRight", calibrateBigRight);
            calibrateNode.appendChild(calibrateAllNode);
            calibrateNode.appendChild(calibrateBigLeftNode);
            calibrateNode.appendChild(calibrateBigRightNode);

            ParseNode readNode = new ParseNode("read", printUsage);
            ParseNode readAllNode = new ParseNode("all", readAll);
            ParseNode readBigLeftNode = new ParseNode("bigLeft", readBigLeft);
            ParseNode readBigRightNode = new ParseNode("bigRight", readBigRight);
            readNode.appendChild(readAllNode);
            readNode.appendChild(readBigLeftNode);
            readNode.appendChild(readBigRightNode);

            ParseNode saveNode = new ParseNode("save", printUsage);
            ParseNode saveAllNode = new ParseNode("all", saveAll);
            ParseNode saveBigLeftNode = new ParseNode("bigLeft", saveBigLeft);
            ParseNode saveBigRightNode = new ParseNode("bigRight", saveBigRight);
            saveNode.appendChild(saveAllNode);
            saveNode.appendChild(saveBigLeftNode);
            saveNode.appendChild(saveBigRightNode);


            parseTree.appendChild(calibrateNode);
            parseTree.appendChild(readNode);
            parseTree.appendChild(saveNode);
        }
        #endregion

        #region Properties
        public static ConsoleArgumentsHandler Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ConsoleArgumentsHandler();
                }
                return instance;
            }
        }
        #endregion

        #region Methods
        private void printUsage()
        {
            Console.WriteLine("Incorrect instruction! Please use");
            printTree(parseTree, 0);
        }

        private void printTree(ParseNode root, int numOfTabs)
        {
            foreach (ParseNode node in root.getChildren())
            {
                if (node.height > 1)
                {
                    printTree(node, numOfTabs + 1);
                }
                else if (node.height == 1)
                {
                    printNode(node, numOfTabs);
                }
                else if (node.height == 0)
                {
                    Console.WriteLine("{0}{1}", numOfTabsToString(numOfTabs), node.ToString());
                }
            }
        }

        private void printNode(ParseNode root, int numOfTabs)
        {
            Console.WriteLine("{0}{1}", numOfTabsToString(numOfTabs), root.ToString());
            Console.Write("{0}", numOfTabsToString(numOfTabs + 1));
            foreach (ParseNode node in root.getChildren())
            {
                Console.Write("{0} \\ ", node.ToString());
            }
            Console.Write("\n\nOR\n\n");
        }

        private string numOfTabsToString(int numOfTabs)
        {
            return String.Concat(Enumerable.Repeat("\t", numOfTabs * 2));
        }

        private void calibrateAll()
        {
            calibrateBigLeft();
            calibrateBigRight();
        }

        private void calibrateBigLeft()
        {
            RPiConsole.sensorBigLeft.calibrate();
            Console.WriteLine("{0}", Math.Round(RPiConsole.sensorBigLeft.Coefficient, 2).ToString());
        }

        private void calibrateBigRight()
        {
            RPiConsole.sensorBigRight.calibrate();
            Console.WriteLine("{0}", Math.Round(RPiConsole.sensorBigRight.Coefficient, 2).ToString());
        }

        private void readAll()
        {
            readBigLeft();
            readBigRight();
        }

        private void readBigLeft()
        {
            Console.WriteLine("{0}", RPiConsole.sensorBigLeft.read());
        }

        private void readBigRight()
        {
            Console.WriteLine("{0}", RPiConsole.sensorBigRight.read());
        }

        private void saveAll()
        {
            saveBigLeft();
            saveBigRight();
        }

        private void saveBigLeft()
        {
            double read = RPiConsole.sensorBigLeft.read();
            // TODO Michael: implement correctly
        }

        private void saveBigRight()
        {
            double read = RPiConsole.sensorBigRight.read();
            // TODO Michael: implement correctly        
        }

        public void parseLine(string line)
        {
            string[] tokens = line.Split(null);
            parseTokens(tokens, parseTree);
        }

        private void parseTokens(string[] tokens, ParseNode root)
        {
            if(tokens.Length == 0)
            {
                root.action();
                return;
            }

            foreach (ParseNode node in root.getChildren())
            {
                if (node.token == tokens[0])
                {
                    parseTokens(tokens.Skip(1).ToArray(), node);
                    return;
                }
            }

            printUsage();
        }

        #endregion

        private class ParseNode
        {
            #region Fields
            private List<ParseNode> children;
            public string token;
            public Action action;
            public int height;
            #endregion

            #region Constructors
            private ParseNode(string token)
            {
                this.token = token;
                this.children = new List<ParseNode>();
                this.height = 0;
            }

            public ParseNode(string token, Action action)
                :this(token)
            {
                this.action = action;
            }
            #endregion

            #region Methods
            public override string ToString()
            {
                return token;
            }

            public void appendChild(ParseNode child)
            {
                children.Add(child);

                int childHeight = child.height;
                if (childHeight + 1 > height)
                    height = childHeight + 1;
            }

            public List<ParseNode> getChildren()
            {
                return children;
            }
            #endregion
        }
    }
}