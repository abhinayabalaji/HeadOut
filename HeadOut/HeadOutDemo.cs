using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using System;
using System.Runtime;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;


namespace HeadOutDemo
{
    class HeadOutDemo
    {

        struct SeatDetail
        {
            public string Color;
            public string SeatNumber;
            public double SeatCost;
            public int LeftCoord;
            public int TopCoord;

            public SeatDetail(string color, string seatNumber, double seatCost, int leftCoord, int topCoord)
            {
                Color = color;
                SeatNumber = seatNumber;
                SeatCost = seatCost;
                LeftCoord = leftCoord;
                TopCoord = topCoord;

            }
        }


        Dictionary<string, List<SeatDetail>> dictSeatSelection = new Dictionary<string, List<SeatDetail>>();
        Dictionary<string, int> randomPaxPerCategory = new Dictionary<string, int>();

        IWebDriver driver;        
        string CurrentWindow = null;

        [SetUp]
        public void startBrowser()
        {
            driver = new ChromeDriver(@"C:\Users\AbhinayaBalaji\Documents");
            driver.Manage().Window.Maximize();
            string LandingPageUrl = "https://www.londontheatredirect.com/";
            driver.Url = LandingPageUrl;
            CurrentWindow = driver.CurrentWindowHandle;            
        }

        [Test]
        public void HitLandingPage()
        {
            string LandingPageUrl = "https://www.londontheatredirect.com/";
            driver.Url = LandingPageUrl;
        }

        [Test]
        public void BookShow()
        {
            try
            {
                driver.SwitchTo().Window(CurrentWindow);

                //check if mamma mia show is present
                //to do --- make it case insensitive
                string showElementPath = "//a[contains(@data-event-name,'Mamma Mia')]";
                var showElement = GetWebElement(By.XPath(showElementPath));
                Assert.True(showElement != null);
                showElement.Click();

                //click on book tickets
                string bookTicketsId = "ctl00_MainContent_BookingBoxControl_BookButtonHL";
                WebDriverWait wait = new WebDriverWait(driver, new TimeSpan(0, 0, 20));
                wait.Until(dr => dr.FindElement(By.Id(bookTicketsId)).Enabled);
                var bookTicketsElement = GetWebElement(By.Id(bookTicketsId));
                Assert.True(bookTicketsElement != null);
                bookTicketsElement.Click();

                //choose and click on the first available show one week from current date
                string strShowDateTime = GetBookingDate();
                string dateElementPath = $"//a[contains(@data-performance,'{strShowDateTime}')]";
                var dateElement = GetWebElement(By.XPath(dateElementPath));
                Assert.True(dateElement != null);
                dateElement.Click();

                //needs fix -- currently put a sleep for 5 seconds
                //loading seats takes a while, look for an element on the page

                //wait = new WebDriverWait(driver, new TimeSpan(0, 0, 10));
                //wait.Until(dr => dr.FindElement(By.XPath("//div[@id='PlanCanvas']/div/div/canvas")).Enabled);

                Thread.Sleep(5000);

                //check if the seat arrangement canvas element is present
                var canvasElement = GetWebElement(By.XPath("//div[@id='PlanCanvas']/div/div/canvas"));
                Assert.True(canvasElement != null);

                //get the canvas dimensions -- w for width, h for height
                var w = canvasElement.Size.Width;
                var h = canvasElement.Size.Height;

                //selection of seats is by moving over the canvas and based on the tooltip that shows for each seat
                //get tooltip element
                var tooltipElement = GetWebElement(By.ClassName("ltd-seatplan__tooltip"));
                Assert.True(tooltipElement != null);

                //incrementing by 15 px -- hardcoded pixel value -- can be made better
                for (int i = 1; i < h; i = i + 15)
                {
                    // starting from the centre
                    // limitation -- seats to my right will only be booked in this case -- can be made better
                    //if there is a logic behind canvas display that can be tested in code, this is more of blackbox testing
                    for (int j = w / 2; j < w * 0.75; j = j + 15)
                    {
                        Actions actions = new Actions(driver);
                        actions.MoveToElement(canvasElement, j, i);
                        actions.Build();
                        actions.Perform();

                        var classes = tooltipElement.GetAttribute("class");

                        //hidden -- means unavailable seat
                        if (classes.Contains("hidden"))
                        {
                            continue;
                        }

                        else
                        {
                            SeatSelection(canvasElement, i, j, classes);
                        }
                    }
                }

                //dictSeatSelection holds the seats to be booked with the coords in the canvas                
                //in the loop calculate the total number of tickets and total cost to verify against the booking summary
                int totalTicketCount = 0;
                double totalTicketCost = 0;

                foreach (var seat in dictSeatSelection.Values)
                {
                    List<SeatDetail> seatDetails = seat;
                    foreach (var seatDetail in seatDetails)
                    {                        
                        totalTicketCount = totalTicketCount + 1;
                        totalTicketCost = totalTicketCost + seatDetail.SeatCost;
                    }
                }

                //check the booking summary pane with the values of dictSeatSelection, totalTicketCount, totalTicketCost
                //class --- seat-plan__basket__item pll2

                ValidateBookingSummary(totalTicketCount, totalTicketCost);

                //SeatPlanSubmit
                //submit button takes a second to get clickable
                string submitTickets = "SeatPlanSubmit";
                var submitTicketsElement = GetWebElement(By.Id(submitTickets));
                Assert.True(submitTicketsElement != null);

                //wait = new WebDriverWait(driver, new TimeSpan(0, 0, 20));
                //wait.Until(dr => dr.FindElement(By.Id(submitTickets)).Enabled);                
                submitTicketsElement.Click();

                //missing test case for ticket protection value validation

                //enter credit card information
                //class -- input-container__row input-container__composite
                //methodCard
                Thread.Sleep(5000);

                string methodCardPath = "//label[@data-method='methodCard']";                
                string namePath = "//input[@name='name']";
                string mailPath = "//input[@name='email']";
                string telPath = "//input[@name='tel']";
                string cardNumPath = "//input[@name='card']";
                string expDatePath = "//input[@name='expiry']";
                string cvvPath = "//input[@name='cvv']";                

                IWebElement inputElement = GetWebElement(By.XPath(methodCardPath));                
                inputElement.Click();

                inputElement = GetWebElement(By.XPath(namePath));
                inputElement.Click();
                inputElement.SendKeys("abcd efg");

                inputElement = GetWebElement(By.XPath(mailPath));
                inputElement.SendKeys("check@check.com");

                inputElement = GetWebElement(By.XPath(telPath));
                inputElement.SendKeys("1234567890");

                inputElement = GetWebElement(By.XPath(cardNumPath));
                inputElement.SendKeys("4242424242424242");

                inputElement = GetWebElement(By.XPath(expDatePath));
                inputElement.SendKeys("03/22");

                inputElement = GetWebElement(By.XPath(cvvPath));
                inputElement.SendKeys("100");                

                inputElement = GetWebElement(By.ClassName("ltd-button__row"));
                inputElement.Click();                
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }

        private void BookSeat(IWebElement canvasElement, SeatDetail seatDetail)
        {
            Actions actionClick = new Actions(driver);
            actionClick.MoveToElement(canvasElement, seatDetail.LeftCoord, seatDetail.TopCoord);
            actionClick.Click();
            actionClick.Build();
            actionClick.Perform();
        }

        [TearDown]
        public void closeBrowser()
        {
            driver.Close();
        }



        //helper methods -- split it out later in to a helper class

        private bool IsElementDisplayed(By by)
        {
            bool flag = false;
            try
            {
                flag = driver.FindElement(by).Displayed;
                return flag;
            }
            catch (NoSuchElementException)
            {
                return flag;
            }
        }

        private IWebElement GetWebElement(By by)
        {
            try
            {
                var webElement = driver.FindElement(by);
                return ((webElement != null) ? webElement : null);
            }
            catch (NoSuchElementException ex)
            {
                Assert.Fail(ex.Message);
                return null;
            }
        }

        private SeatDetail ConstructSeat(string classes, int height, int width)
        {
            string[] tempClass = classes.Split(' ');
            string color = tempClass[tempClass.Length - 1];
            int leftCoord = width;
            int topCoord = height;
            var seatDetailsClass = driver.FindElement(By.ClassName("ltd-seatplan__tooltip__seat"));
            string seatNumber = seatDetailsClass.Text;
            var seatCostClass = driver.FindElement(By.ClassName("ltd-seatplan__tooltip__seatdetails"));
            string seatCost = seatCostClass.Text;
            //splitting to get rid of the extra information provided on a few seats
            string[] tempCost = seatCost.Split('\r');
            seatCost = string.Empty;
            seatCost = tempCost[0];
            //remove the first character which holds the currency symbol
            seatCost = seatCost.Trim(new char[] { '$', '€', '¢', '£', '¥', '₽', '₪', '฿', '₴', '₫', '₹' });

            SeatDetail seatDetail = new SeatDetail(color, seatNumber, Convert.ToDouble(seatCost), leftCoord, topCoord);
            return seatDetail;
        }

        private void SeatSelection(IWebElement canvasElement, int i, int j, string classes)
        {            
            SeatDetail seatDetail = ConstructSeat(classes, i, j);
            if (dictSeatSelection.ContainsKey(seatDetail.Color))
            {
                //add seat selection for an already existing seat category
                List<SeatDetail> existingSeats = dictSeatSelection[seatDetail.Color];
                int tmpCount = randomPaxPerCategory[seatDetail.Color];

                //check with the max pax value
                if (existingSeats.Contains(seatDetail) == false && tmpCount > existingSeats.Count)
                {
                    existingSeats.Add(seatDetail);
                    BookSeat(canvasElement, seatDetail);
                }
            }
            else
            {
                //add the color with a random generated pax count for that seat category
                Random randomPaxCount = new Random();
                int paxCount = randomPaxCount.Next(1, 3);
                randomPaxPerCategory.Add(seatDetail.Color, paxCount);

                //add a fresh category with a new list of seats
                List<SeatDetail> newSeats = new List<SeatDetail>();
                newSeats.Add(seatDetail);
                dictSeatSelection.Add(seatDetail.Color, newSeats);
                BookSeat(canvasElement, seatDetail);
            }
        }

        private string GetBookingDate()
        {
            //booking tickets for a week from today
            //sundays there are no shows - so if dayofweek is sunday add 8 days, else add 7 days
            //date expression example ----  Friday 11 October 2019 - 19:45
            DateTime showDateTime = new DateTime();
            string strShowDateTime = null;

            if (DateTime.Today.DayOfWeek.Equals(DayOfWeek.Sunday))
            {
                showDateTime = DateTime.Today.AddDays(8);
            }
            else
            {
                showDateTime = DateTime.Today.AddDays(7);
            }

            //example 17 October 2019
            strShowDateTime = showDateTime.ToString("dd MMMM yyyy");
            return strShowDateTime;
        }

        private void ValidateBookingSummary(int totalTicketCount, double totalTicketCost)
        {
            foreach (var item in dictSeatSelection)
            {
                string basketItem = $"//div[contains(@class, 'seat-plan__basket__item') and contains(@class, '{item.Key}')]";
                IReadOnlyCollection<IWebElement> bookedSeats = driver.FindElements(By.XPath(basketItem));
                if (bookedSeats.Count != item.Value.Count)
                {
                    Assert.Fail("Count of selected seats in this category mismatches with the booking summary");
                }
            }

            string seatPlan = "//a[@id='SeatPlanSubmit']/small";
            var seatPlanElement = GetWebElement(By.XPath(seatPlan));
            Assert.True(seatPlanElement != null);
            string[] seatPlanElementText = seatPlanElement.Text.Split(' ');

            if (totalTicketCount != Convert.ToInt32(seatPlanElementText[0]))
                Assert.Fail("Mismatch in the total ticket count between selection and booking summary");

            seatPlan = "//a[@id='SeatPlanSubmit']/small/strong";
            seatPlanElement = GetWebElement(By.XPath(seatPlan));
            Assert.True(seatPlanElement != null);
            double totalCost = Convert.ToDouble(seatPlanElement.Text.Substring(1, seatPlanElement.Text.Length - 1));

            if (totalCost != totalTicketCost)
                Assert.Fail("Mismatch in the total ticket cost between selection and booking summary");
        }
    }
}