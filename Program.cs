using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Net;
using System.IO;
using System.Collections;


namespace ZipCodeGrab
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting...");
            Guantlet();
        }

        private static void Guantlet()
        {
            ArrayList InputArrList = new ArrayList();
            List<string> OutputList = new List<string>();
            string[] myTypeArr = {"postal_code",  "country", "locality", "administrative_area_level_1", 
                                        "administrative_area_level_2", "sublocality_level_1" };
            OutputList.add(myTypeArr);
            InputArrList = ImportExportCSV.grabCsvData("C:\\users\\fargusonm\\desktop\\Matt.txt", "Test", '|');
            string myUrl = @"http://maps.googleapis.com/maps/api/geocode/json?address=";
            string myUrlSensor = "&sensor=true";
            int myCount = InputArrList.Count;
            int i = 0;
            while (i < myCount)
            {
                object currentLine = InputArrList[i];
                string[] InputArr = currentLine as string[];
                string zip = InputArr[0];
                string country = InputArr[0];
                string myFullUrl = myUrl + zip + myUrlSensor;
                JSonDataParse jsdp = new JSonDataParse(myFullUrl, country, "address_components", "long_name");
                if (jsdp.DataOK)
                {
                    
                    OutputArrList.Add(jsdp.ReturnData);
                    Console.Write("\r{0}  -count    ", i.ToString());
                }
                
                i++;
            }
            ImportExportCSV.outputResults(OutputArrList, "C:\\users\\fargusonm\\desktop\\MattsTest.txt"); 
            Console.WriteLine();

        }
    public class JSonDataParse
    {

        private string country { get; set; }
        private string cutDownJSON { get; set; }
        private string jSonURL { get; set; }
        private string jSonObj { get; set; }
        private string fieldName { get; set; }
        private string componentName { get; set; }
        List<string> ReturnData { get; set; }
        public bool DataOK { get; set; }

        public JSonDataParse(string JSonURL, string Country, string ComponentName, string FieldName)
        {
            //object_constructor
            country = Country;
            jSonURL = JSonURL;
            fieldName = FieldName;
            componentName = ComponentName;
            cutDownJSON = "";
            DataOK = true;
            JsonClean();
        }

        private void JsonClean()
        {
            getJSONData();
            cleanJSONdata("\"results\" : [");
            cutDownJSONData(); //updates member that cutdownjson
            jSonObj = jSonObj.Substring(cutDownJSON.Length, jSonObj.Length - cutDownJSON.Length);
            if (cutDownJSON != "")
            {
                getBlockData();
            }
            else
            {
                DataOK = false;
            }
        }

        private void cleanJSONdata(string StartString)
        {
            //chops off the first portion of Json for further cleaning after start string
            int myStartPoint = StringUtils.Search(jSonObj, StartString);
            jSonObj = jSonObj.Substring(myStartPoint + StartString.Length);

        }
        private void getJSONData()
        {
            //runs first
            string results = "";
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(jSonURL);
                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
                StreamReader sr = new StreamReader(resp.GetResponseStream());
                results = sr.ReadToEnd();
                sr.Close();
                resp.Close();
            }
            catch (Exception x)
            {
                results = x.Message;
            }
            jSonObj = results;
        }

        private void cutDownJSONData()
        { //runs second

            int i = 1;
            int jcCutDownStart = 1;
            while (jcCutDownStart > 0)
            {
                try
                {
                    jcCutDownStart = StringUtils.Search(jSonObj, componentName, i);
                    string CutDownJSON = jSonObj.Substring(jcCutDownStart,
                                                           StringUtils.Search(jSonObj, "],") - jcCutDownStart);

                    bool CountryInBlock = StringUtils.CountString(CutDownJSON, country) > 0;
                    if (CountryInBlock)
                    {
                        cutDownJSON = CutDownJSON;
                        break;
                    }

                    i++;
                }
                catch 
                {
                    break;
                }
            }
        }

        private void getBlockData()
        {
            string newBlock = cutDownJSON;
            string[] outArr = { "", "", "", "", "", "" };
            string[] myTypeArr = {"postal_code",  "country", "locality", "administrative_area_level_1", 
                                        "administrative_area_level_2", "sublocality_level_1" };
            int inst = 1;
            int startPoint = 1;
            int stopPoint = 1;
            while (startPoint > 0)
            {
                startPoint = StringUtils.Search(newBlock, "{", inst);
                stopPoint = StringUtils.Search(newBlock, "}", inst);
                string tempBlock = newBlock.Substring(startPoint, stopPoint - startPoint);
                int i = 0;
                while (i < myTypeArr.Length)
                {
                    if (StringUtils.CountString(tempBlock, myTypeArr[i]) > 0)
                    {
                        int startPointGranular = StringUtils.Search(tempBlock, fieldName) + fieldName.Length + 4;
                        int endPointGranular = StringUtils.Search(tempBlock.Substring(startPointGranular), ",") - 2;
                        outArr[i] =  tempBlock.Substring(startPointGranular, endPointGranular);
                        break;
                    }
                        
                        i++;
                }
                inst++;
            }
            ReturnData.AddRange(outArr);

        }
    }
        public static class ImportExportCSV
        {

            public static ArrayList grabCsvData(string fileName, string tableName, char splitChar)
            {
                /*pulls data from from file name and converts into an array list for future loops*/
                StreamReader sr = new StreamReader(fileName);
                string x = sr.ReadLine();
                ArrayList myList = new ArrayList();
                string[] rows = x.Split(splitChar);
                myList.Add(rows);
                int myInterval = rows.Count();
                string currentLine = sr.ReadLine();
                string[] currentArray = currentLine.Split(splitChar);

                while (currentArray != null)
                {
                    int currentLength = currentArray.Length;
                    if (currentLength != myInterval)
                    {
                        while (currentLength < myInterval)
                        {
                            string newLine = sr.ReadLine();
                            currentLine = currentLine + newLine;
                            currentArray = currentLine.Split(splitChar);
                            currentLength = currentArray.Length;
                        }
                    }
                    myList.Add(currentArray);
                    currentLine = sr.ReadLine();
                    if (currentLine == null) { break; }
                    currentArray = currentLine.Split(splitChar);
                }
                sr.Dispose();
                return myList;
            }

            public static void outputResults(ArrayList results, string filePath)
            {
                /*dump results in a new csv file split by | symbol*/
                Console.WriteLine("Dumping results -- here: " + filePath);
                var csv = new StringBuilder();
                int i = System.IO.File.Exists(filePath) ? 1 : 0;
                while (i < results.Count)
                {
                    object arrObj = results[i];
                    string[] myResultsArr = arrObj as string[];

                    csv.Append(myResultsArr[0] + "|" + myResultsArr[1] + "|" + myResultsArr[2]
                            + "|" + myResultsArr[3] + "|" + myResultsArr[4] + "|" + myResultsArr[5]);
                    csv.Append(Environment.NewLine);

                    i++;

                }
                File.AppendAllText(filePath, csv.ToString());
            }
        }
        }

        public static class StringUtils
        {
            public static int Search(string yourString, string yourMarker, int yourInst = 1, bool caseSensitive = true)
            {
                //returns the placement of a string in another string
                int num = 0;
                int ginst = 1;
                int mlen = yourMarker.Length;
                int slen = yourString.Length;
                string tString = "";
                try
                {
                    if (caseSensitive == false)
                    {
                        yourString = yourString.ToLower();
                        yourMarker = yourMarker.ToLower();
                    }
                    while (num < slen)
                    {
                        tString = yourString.Substring(num, mlen);

                        if (tString == yourMarker && ginst == yourInst)
                        {
                            return num + 1;
                        }
                        else if (tString == yourMarker && yourInst != ginst)
                        {
                            ginst += 1;
                            num += 1;
                        }
                        else
                        {
                            num += 1;
                        }
                    }
                    return 0;
                }
                catch
                {
                    return 0;
                }
            }
            public static int CountString(string yourString, string yourMarker)
            {
                //counts the number of strings that exist in another string
                int myCnt = 0;
                string newstring = yourString.Replace(yourMarker, "");
                myCnt = (yourString.Length - newstring.Length) / yourMarker.Length;
                return myCnt;
            }       
        }    
}

