using LiveCharts;
using LiveCharts.WinForms;
using LiveCharts.Wpf;
using SkiaSharp;
using Newtonsoft;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Windows.Media;
using System.Xml.Linq;
namespace CURSE
{
    public partial class Form1 : Form
    {
        #region Vals
        private HttpClient _client;
        private string _URL = "https://www.cbr-xml-daily.ru/latest.js";
        public List<List<(string, decimal)>> Values = [];
        #endregion

        public Form1()
        {
            InitializeComponent();
            this.MaximizeBox = false;            
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this._client = new();
            dataGridView1.ColumnCount = 2;
            dataGridView1.Columns[0].Name = "Валюта";
            dataGridView1.Columns[1].Name = "Значение";            
            cartesianChart1.AxisX.Add(new Axis
            {
                LabelFormatter = val => DateTime.Now.ToString()
            });
            cartesianChart1.Zoom = ZoomingOptions.Y;
            cartesianChart1.DataTooltip.ToolTip = null;
            Refresher();
        }
        async Task<string> Refresher()
        {
            var first = true;
            while (true)
            {
                try
                {
                    var Responce = await _client.GetAsync(_URL);
                    if (Responce.IsSuccessStatusCode)
                    {
                        listBox1.Items.Clear();
                        listBox2.Items.Clear();
                        
                        listBox1.Items.Add("RU");
                        listBox2.Items.Add("RU");
                        
                        
                        string result = await Responce.Content.ReadAsStringAsync();
                   
                        var obj = JsonConvert.DeserializeObject<dynamic>(result);
                        List<(string, decimal)> newvals = [("RU",1m)];
                        foreach (dynamic item in obj.rates)
                        {
                            try
                            {
                                string name = item.Name;
                                decimal val = Convert.ToDecimal(item.Value);
                                listBox1.Items.Add(name);
                                listBox2.Items.Add(name);
                                newvals.Add((name, val));
                                if (first)
                                {
                                    cartesianChart1.Series.Add(
                                        new LineSeries
                                        {
                                            Title = name,
                                            Values = new ChartValues<decimal> { val },
                                            Stroke = null
                                        }
                                        );
                                }
                                else
                                {
                                    cartesianChart1.Series.Where(x=> x.Title == name).First().Values.Add(val);
                                }
                            }
                            catch (Exception exc)
                            {
                                throw new Exception($"Ошибка при парсинге значений, {exc.Message}");
                            }
                        }
                        Values.Add(newvals);
                        dataGridView1.Rows.Clear();
                        foreach (var item in newvals)
                        {
                            dataGridView1.Rows.Add(item.Item1, item.Item2);             
                        }
                        first = false;                        
                    }
                    else
                    {
                        throw new HttpRequestException("Ошибка при обращении к сайту!");
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show($"Ошибка при обновлении значений: {e.Message}");
                }
                //METHODS UP
                await Task.Delay(60 * 1000);
            }
        }
        private class CONTENT
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (listBox1.SelectedItems is null || listBox2.SelectedItems is null)
                {
                    throw new Exception("Выберите валюту!");
                }
                var valsum = numericUpDown1.Value;
                string name1 = listBox1.SelectedItem.ToString();
                string name2 = listBox2.SelectedItem.ToString();
                decimal price1 = (from item in Values.Last() where item.Item1 == name1 select item.Item2).First();
                decimal price2 = (from item in Values.Last() where item.Item1 == name2 select item.Item2).First();
                decimal resultvalue;
                if (name1 == "RU")
                {
                    resultvalue = valsum * price2;
                }
                else if (name2 == "RU")
                { 
                    resultvalue = valsum * 1/price1;
                }
                else
                {
                    resultvalue = (valsum) * (price1 / price2);
                }
                MessageBox.Show($"Отношение {valsum} {name1} к {name2} на последний момент составляет: {Math.Abs(resultvalue)}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            { 
                listBox1.SelectedItems.Clear();
                listBox2.SelectedItems.Clear();
            }
            //MessageBox.Show(val1 + ' ' + val2);

        }
    }
}
