using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.IO;                                        // Для работы с файлами
using System.Threading;                                 // Для времени
using Sharp7;                                           // Для работы с контролерами Simens S300, S400, S1000
using System.Windows.Forms.DataVisualization.Charting;  // Для отрисовки графика

namespace TestGrafics
{       
    public partial class Form1 : Form
    {
        #region Глобальные переменные
        #region [Набор элементов для формиорования и считывания даных]
        //Обьявление клиента
        S7Client Client = new S7Client();
        string IP;
        int RACK;
        int SLOT;
        //Переменная для тестового соединения
        int Result;
        //Обьявляю массивы для установки размера блоков данных
        byte[] Buffer_num001;
        byte[] Buffer_num002;
        //Переменные для числового значения массива
        int num_size_001 = 0; 
        int num_size_002 = 0;
        //Номер
        int DBNum_001;
        int DBNum_002;
        //Позиция
        int DB_Pos_001;
        int DB_Pos_002;
        //Бит (для квадратных линий)
        int DB_Bit_001;
        int DB_Bit_002;
        //Массивы для записи данных
        int[] Proba_int = new int[3];               //DBW
        double[] Proba_double = new double[3];      //DBD
        string[] Proba_ot = new string[3];
        #endregion

        #region [Набор переменных для оформления апликации и логики обработки даных] 
        OpenFileDialog MainPreset = new OpenFileDialog();       // Для окна выбора пресета
        SaveFileDialog MainSavePreset = new SaveFileDialog();   // Для окна сохранения пресета

        Point? prevPosition = null;                             // Структура для элемента Chart
        ToolTip tooltip = new ToolTip();                        // Tooltip - всплывающие окно (для элемента Chart)

        string Limit_path = (@"");
        string _set_pres = "";
        string preset = "";
        string limit_otch = "";

        bool Connect_disconect = false;                         // true - есть связь / false - отсуствует связь
        bool locker_tb = true;                                  // Для блокировки текст боксов
        bool No_sleep = false;                                  // Для отключения задершки (Sleep)
        bool count_1_ok = false;                                // Для установки подсчёта канкулятора
        bool count_2_ok = false;                                // Для блокировка подсчёта канкулятора
        bool Add_all_count = false;                             // 
        bool locker_set_reset = true;                           // Для дополнительной блокировки кнопки "установить"

        byte time = 0;                                          // Для рабочего времени(Work_Time)

        int Saw_Control_act_1 = 0;                              // Для тестового режима (1.Число)
        int Saw_Control_act_2 = 0;                              // Для тестового режима (2.Число)

        int Limit = 0;                                          // Для установки лимита
        int count_1 = 0;                                        // Для подсчёта превышения лимита (для обоих чисел или только для первого числа в режиме посчёта "2 - Считать отдельно")
        int count_2 = 0;                                        // Для подсчёта превышения лимита (только для второго числа в режиме посчёта "2 - Считать отдельно")
        int count_all = 0;                                      // Для хранения общего количества превышений лимита

        int res;

        int N;
        int Ni;
        int X = 0;

        #endregion
        #endregion

        public Form1()
        {
            InitializeComponent();
            N = 100;
            decrem(Ni);

            IP = Convert.ToString(tb_ip.Text);
            RACK = Convert.ToInt32(tb_rack.Text);
            SLOT = Convert.ToInt32(tb_slot.Text);

            chart1.Series[0].LegendText = "Имя_1";
            chart1.Series[1].LegendText = "Имя_2";

            chart1.Series[2].IsVisibleInLegend = false;

            btn_set_reset.Enabled = false;
            btn_stop.Visible = false;
            tabControl1.Enabled = false;
            chb_auto_otch.Enabled = false;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Interface_Time.Start();

            chart1.ChartAreas[0].AxisX.ScaleView.Zoom(0, 10);
            chart1.ChartAreas[0].CursorX.IsUserEnabled = true;
            chart1.ChartAreas[0].CursorX.IsUserSelectionEnabled = true;
            chart1.ChartAreas[0].AxisX.ScaleView.Zoomable = true;
            chart1.ChartAreas[0].AxisX.ScrollBar.IsPositionedInside = true;

            chart1.ChartAreas[0].AxisY.ScaleView.Zoom(0, 100);
            chart1.ChartAreas[0].CursorY.IsUserEnabled = true;
            chart1.ChartAreas[0].CursorY.IsUserSelectionEnabled = true;
            chart1.ChartAreas[0].AxisY.ScaleView.Zoomable = true;
            chart1.ChartAreas[0].AxisY.ScrollBar.IsPositionedInside = true;

            chart1.Series[0].BorderWidth = 3;
            chart1.Series[1].BorderWidth = 3;
            chart1.Series[2].BorderWidth = 3;

            lb_inter_time.Text = "Интервал чтения:" + Convert.ToString(scb_timer.Value) + "ms.";
            btn_preset_creat.Enabled = false;
            btn_prset_set.Enabled = false;
            btn_start.Enabled = false;           
        }

        #region Набор общих методов
        int decrem(int i)
        {
            i--;
            return i;
        }
        // ОтоброжениямПанелей
        void visual_panel(Panel pn)
        {
            if (pn.Visible == false)
            {
                pn.Visible = true;
            }
            else
            {
                pn.Visible = false;
            }
        }
        // Вывот результата
        private void ShowResult(int Resultat)
        {
            lb_info.Text = "";
            if (Resultat == 0)
            {
                lb_info.Text = "Подключение активно:" + " (" + Client.ExecutionTime.ToString() + " ms)";
            }
            else
            {
                lb_info.Text = "Нет связи!";
            }
        }
        // Чтения целых чисел
        void Main_Mon_Read(int nub_DB, byte[] Buffer, int Pos, ref int Znach)
        {
            int result = Client.DBRead(nub_DB, 0, Buffer.Length, Buffer);
            Znach = S7.GetIntAt(Buffer, Pos);
        }
        // Чтения дробных чисел
        void Main_Mon_Read_no_int(int nub_DB, byte[] Buffer, int Pos, ref double Znach)
        {
            int result = Client.DBRead(nub_DB, 0, Buffer.Length, Buffer);
            Znach = S7.GetRealAt(Buffer, Pos);
        }
        // Коректировка формата
        void corect_format(TextBox a)
        {
            switch (a.Text)
            {
                //DBW
                case "dbw":
                    a.Text = "DBW";
                    break;

                case "Dbw":
                    a.Text = "DBW";
                    break;

                case "DBw":
                    a.Text = "DBW";
                    break;

                case "dBw":
                    a.Text = "DBW";
                    break;

                case "dBW":
                    a.Text = "DBW";
                    break;

                case "dbW":
                    a.Text = "DBW";
                    break;
                //DBD
                case "dbd":
                    a.Text = "DBD";
                    break;

                case "Dbd":
                    a.Text = "DBD";
                    break;

                case "DBd":
                    a.Text = "DBD";
                    break;

                case "dBd":
                    a.Text = "DBD";
                    break;

                case "dBD":
                    a.Text = "DBD";
                    break;

                case "dbD":
                    a.Text = "DBD";
                    break;
            }
        }
        // Для Блокировки текст Боксов в (динамике Work_Time)
        void Main_locker_btn()
        {
            if (locker_set_reset == true)
            {
                if (tb_001_Name.Text == "" || tb_001NumDB.Text == "" || tb_001_sizeDB.Text == "" || tb_001PosDB.Text == "" || tb_001FormDB.Text == "")
                {
                    btn_set_reset.Enabled = false;
                }
                else
                {
                    btn_set_reset.Enabled = true;
                }

                if(tb_002_Name.Text == "" || tb_002NumDB.Text == "" || tb_002_sizeDB.Text == "" || tb_002PosDB.Text == "" || tb_002FormDB.Text == "")
                {
                    Cleaner_tb(tb_002FormDB, tb_002NumDB, tb_002PosDB, tb_002_Name, tb_002_sizeDB);
                }
            }
        }
        // Для определения статуса ЦПУ
        void CPU_Status()
        {
            int PLCStatus = 0;
            Client.PlcGetStatus(ref PLCStatus);
            switch (PLCStatus)
            {
                case 0x08:
                    lb_info.Text = "Статус контролера: RUN";
                    pn_Top_Left_led_onlin.BackColor = System.Drawing.Color.Green;
                    break;
                case 0x04:
                    lb_info.Text = "Статус контролера: STOP";
                    pn_Top_Left_led_onlin.BackColor = System.Drawing.Color.Gold;
                    break;
                default:
                    lb_info.Text = "Статус контролера: Не определён (Отключитесь и попробуйте заново подключиться)";
                    pn_Top_Left_led_onlin.BackColor = System.Drawing.Color.DarkGray;
                    Client.Disconnect();
                    break;
            }
        }
        // Для блокировки текстбоксов на панеле с лева
        void Metod_tb_lock()
        {
            tb_001FormDB.Enabled = locker_tb;
            tb_001NumDB.Enabled = locker_tb;
            tb_001PosDB.Enabled = locker_tb;
            tb_001_Name.Enabled = locker_tb;
            tb_001_sizeDB.Enabled = locker_tb;

            tb_002FormDB.Enabled = locker_tb;
            tb_002NumDB.Enabled = locker_tb;
            tb_002PosDB.Enabled = locker_tb;
            tb_002_Name.Enabled = locker_tb;
            tb_002_sizeDB.Enabled = locker_tb;
        }
        // Для очистки текст бокса (Блок связи)
        void clean_connect(TextBox a)
        {
            if (a.Text == "XXXX")
            {
                a.Text = "";
            }
        }
        // Для чтения присета
        void Reader_preset()
        {
            byte Alarms_Code = 0;
            string filter1 = _set_pres;
            string filter2 = filter1.Substring(0, filter1.IndexOf("."));
            filter1 = filter1.Replace(filter2 + ".", "");

            if (filter1 == "mps")
            {
                try
                {
                    preset = File.ReadAllText(@_set_pres);
                    string[] Main = new string[14];
                    string All;

                    Main[0] = preset.Substring(0, preset.IndexOf(":"));

                    if (Main[0] == "1")
                    {
                        Main[1] = preset.Substring(0, preset.IndexOf("::"));
                        Main[1] = Main[1].Replace(Main[0] + ":", "");
                        tb_001_Name.Text = Main[1];

                        Main[2] = preset.Substring(0, preset.IndexOf(":::"));
                        Main[2] = Main[2].Replace(Main[0] + ":" + Main[1] + "::", "");
                        tb_001NumDB.Text = Main[2];

                        Main[3] = preset.Substring(0, preset.IndexOf("::::"));
                        Main[3] = Main[3].Replace(Main[0] + ":" + Main[1] + "::" + Main[2] + ":::", "");
                        tb_001FormDB.Text = Main[3];

                        Main[4] = preset.Substring(0, preset.IndexOf(":P:"));
                        Main[4] = Main[4].Replace(Main[0] + ":" + Main[1] + "::" + Main[2] + ":::" + Main[3] + "::::", "");
                        tb_001PosDB.Text = Main[4];

                        Main[5] = preset.Substring(0, preset.IndexOf("."));
                        Main[5] = Main[5].Replace(Main[0] + ":" + Main[1] + "::" + Main[2] + ":::" + Main[3] + "::::" + Main[4] + ":P:", "");
                        tb_001_sizeDB.Text = Main[5];

                        chb_001_act.Checked = true;

                        Main[6] = Main[0] + ":" + Main[1] + "::" + Main[2] + ":::" + Main[3] + "::::" + Main[4] + ":P:" + Main[5] + ".";
                        Main[6] = preset.Replace(Main[6], "");
                    }
                    else
                    {
                        Alarms_Code += (byte)(1);
                    }

                    Main[7] = Main[6].Substring(0, Main[6].IndexOf(":"));

                    if (Main[7] == "2")
                    {
                        Main[8] = Main[6].Substring(0, Main[6].IndexOf("::"));
                        Main[8] = Main[8].Replace(Main[7] + ":", "");
                        tb_002_Name.Text = Main[8];

                        Main[9] = Main[6].Substring(0, Main[6].IndexOf(":::"));
                        Main[9] = Main[9].Replace(Main[7] + ":" + Main[8] + "::", "");
                        tb_002NumDB.Text = Main[9];

                        Main[10] = Main[6].Substring(0, Main[6].IndexOf("::::"));
                        Main[10] = Main[10].Replace(Main[7] + ":" + Main[8] + "::" + Main[9] + ":::", "");
                        tb_002FormDB.Text = Main[10];

                        Main[11] = Main[6].Substring(0, Main[6].IndexOf(":P:"));
                        Main[11] = Main[11].Replace(Main[7] + ":" + Main[8] + "::" + Main[9] + ":::" + Main[10] + "::::", "");
                        tb_002PosDB.Text = Main[11];

                        Main[12] = Main[6].Substring(0, Main[6].IndexOf("."));
                        Main[12] = Main[12].Replace(Main[7] + ":" + Main[8] + "::" + Main[9] + ":::" + Main[10] + "::::" + Main[11] + ":P:", "");
                        tb_002_sizeDB.Text = Main[12];

                        chb_002_act.Checked = true;
                    }
                    else
                    {
                        Alarms_Code += (byte)(1);
                    }

                    try
                    {
                        All = Main[0] + ":" + Main[1] + "::" + Main[2] + ":::" + Main[3] + "::::" + Main[4] + ":P:" + Main[5] + "." + Main[7] + ":" + Main[8] + "::" + Main[9] + ":::" + Main[10] + "::::" + Main[11] + ":P:" + Main[12] + ".";

                        string ip_preset = preset.Replace(All, "");
                        ip_preset = ip_preset.Replace("IP:", "");
                        ip_preset = ip_preset.Substring(0, ip_preset.IndexOf("XRack:"));
                        tb_ip.Text = ip_preset;

                        string rack = preset.Replace(All + "IP:" + ip_preset, "");
                        rack = rack.Replace("XRack:", "");
                        rack = rack.Substring(0, rack.IndexOf("XXSlot:"));
                        tb_rack.Text = rack;

                        string slot = preset.Replace(All + "IP:" + ip_preset + "XRack:" + rack, "");
                        slot = slot.Replace("XXSlot:", "");
                        tb_slot.Text = slot;

                        MessageBox.Show("IP:" + ip_preset + "; Rack:" + rack + "; Slot:" + slot);
                    }
                    catch (Exception)
                    {
                        Alarms_Code = 3;
                        lb_info.Text = "1/2 Число заполнены. Не коректно заполнен блок подключения!";
                        tb_ip.Text = "XXXX";
                        tb_rack.Text = "XXXX";
                        tb_slot.Text = "XXXX";
                    }

                }
                catch (Exception eror)
                {
                    lb_info.Text = "Не коректный пресет!!!!!!!!";
                    MessageBox.Show(Convert.ToString(eror));
                }
                finally
                {
                    if (Alarms_Code == 0) MessageBox.Show("Не забудте установить данные!");
                }
            }
        }
        // Очистка текст боксов чисел (1-2)
        void Cleaner_tb (TextBox tb1, TextBox tb2, TextBox tb3, TextBox tb4, TextBox tb5)
        {
            tb1.Text = "";
            tb2.Text = "";
            tb3.Text = "";
            tb4.Text = "";
            tb5.Text = "";
        }
        // Копирование из одногой значения в другой текст-боксов чисел (1-2)
        void copy_tb (TextBox tb1, TextBox tb2, TextBox tb3, TextBox tb4, TextBox tb5, TextBox tb1_1, TextBox tb2_2, TextBox tb3_3, TextBox tb4_4, TextBox tb5_5)
        {
            tb1_1.Text = tb1.Text;
            tb2_2.Text = tb2.Text;
            tb3_3.Text = tb3.Text;
            tb4_4.Text = tb4.Text;
            tb5_5.Text = tb5.Text;
        }
        // Ограничение ввода символа . в поле "Имя обьекта"
        void Name_lock_symbols(KeyPressEventArgs e)
        {
            if (e.KeyChar == '.')
            {
                e.Handled = true;
                lb_info.Text = "Имя обьекта не должно содержать точек [ . ]";
            }
        }
        // Удаление точки из текста (для Имине обьекта)
        void Name_corect(TextBox tb)
        {
            string a = tb.Text;
            a = a.Replace(".", "");
            a = a.Replace(":", "");
            tb.Text = a;
        }
        // Блокировка кнопки "Создать" (Пресет)
        void _locker_btn_creat_preset()
        {
            if (btn_start.Visible == true)
            {
                if (tb_001_Name.Text == "" || tb_001NumDB.Text == "" || tb_001_sizeDB.Text == "" || tb_001PosDB.Text == "" || tb_001FormDB.Text == "" || tb_002_Name.Text == "" || tb_002NumDB.Text == "" || tb_002_sizeDB.Text == "" || tb_002PosDB.Text == "" || tb_002FormDB.Text == "")
                {
                    btn_preset_creat.Enabled = false;
                }
                else
                {
                    btn_preset_creat.Enabled = true;
                }
            }
            else
            {
                btn_preset_creat.Enabled = false;
            }
        }
        // Для подсчёта выхода лимита (!!!!! Alarms)
        void count_limit<T>(T Znach, ref bool ok, ref int count, int limit, TextBox name_obj)
        {
            int conver = Convert.ToInt32(Znach);

            if (chb_limit.Checked == true)
            {
                if (conver > limit && ok == false)
                {
                    count++;
                    ok = true;
                }

                if (limit > conver && ok == true)
                {
                    ok = false;

                    if (chb_auto_otch.Checked == true)
                    {
                        limit_otch += "Лемит был привышен: " + name_obj.Text + "(Дата - " + DateTime.Now.ToShortDateString() + "; Время - " + DateTime.Now.ToLongTimeString() + ");" + "\r\n";
                        using (StreamWriter SH = File.CreateText(Limit_path))
                        {
                            SH.WriteLine(limit_otch);
                        }
                    }  
                }
            }
        }
        // Вывод Лимита на экран
        void out_put_limit()
        {
            lb_count_1.Text = Convert.ToString(count_2 + count_1);
            chart1.Series[2].LegendText = tb_limit_name.Text + ": " + lb_count_1.Text;
        }
        // Для подсчёта привышения лемита одного числа
        void count_main_l (ref int Znach, ref bool ok, ref int count, int limit)
        {
            if (chb_limit.Checked == true)
            {
                if (Znach > limit && ok == false)
                {
                    count++;
                    ok = true;    
                }

                if (limit > Znach && ok == true) ok = false;      
            }
        }
        // Алгоритм работы пилы
        void Alg_sawer(ref int Saw)
        {
            if (Saw == 100)
            {
                Saw = 40;

                switch (Saw)
                {
                    case 50:
                        Saw = 55;
                        break;
                }
            }

            if (Saw > 100)
            {
                Saw = 0;
            }
        }
        // Алг_кнопки старт
        void Start_()
        {
            btn_stop.Visible = true;
            btn_start.Visible = false;
            Reader_Time.Enabled = true;
            locker_tb = false;
            Metod_tb_lock();
            btn_clean_chart.Enabled = false;
            locker_set_reset = false;
            btn_set_reset.Enabled = locker_set_reset;
            btn_prset_set.Enabled = false;
            chb_option_Test_OnOff.Enabled = false;
        }
        // Алг_кнопки стоп
        void Stop_()
        {
            btn_stop.Visible = false;
            btn_start.Visible = true;
            Reader_Time.Enabled = false;
            locker_tb = true;
            Metod_tb_lock();
            btn_clean_chart.Enabled = true;
            if (chb_option_Test_OnOff.Checked == false) locker_set_reset = true;
            btn_set_reset.Enabled = locker_set_reset;
            btn_prset_set.Enabled = true;
            chb_option_Test_OnOff.Enabled = true;
        }
        // Алг кнопки скриншот
        void Screen_shot_()
        {
            using (var bmp = new Bitmap(pn_center.Width, pn_center.Height))
            {
                pn_center.DrawToBitmap(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height));

                SaveFileDialog saf = new SaveFileDialog();
                saf.Title = "Сохранить скриншот как...";
                saf.OverwritePrompt = true;
                saf.CheckPathExists = true;
                saf.Filter = "Image files(*.JPG*)|*.jpg|Image files(*.BMP*)|*.dmp|Image files(*.PNG*)|*.png";

                if (saf.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        bmp.Save(saf.FileName);
                    }
                    catch
                    {
                        MessageBox.Show("Ошибка при сохранение", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        lb_info.Text = "...";
                    }
                }
            }
        }
        #endregion

        #region Динамика
        // Для работы с данными
        private void Reader_Timer_Tick(object sender, EventArgs e)
        { 
            X++;
            N++;
            
            chart1.ChartAreas[0].AxisX.ScaleView.Zoom(X, N);
            chart1.ChartAreas[0].AxisX.Minimum = N - 99;
            chart1.ChartAreas[0].AxisX.Maximum = N;

            if (chb_option_Test_OnOff.Checked == false)
            {
                // Чтение данных
                try
                {
                    switch (tb_001FormDB.Text)
                    {
                        case "":

                            break;

                        case "DBW":
                            Main_Mon_Read(DBNum_001, Buffer_num001, DB_Pos_001, ref Proba_int[0]);

                            if (chb_001_act.Checked == true)
                            {
                                chart1.Series[0].LegendText = tb_001_Name.Text + ": " + Proba_int[0];
                                chart1.Series[0].Points.AddXY(N, Proba_int[0]);
                                lb_001_Monit.Text = Convert.ToString(Proba_int[0]);

                               if(chb_limit.Checked == true) count_limit<int>(Proba_int[0], ref count_1_ok, ref count_1, Limit, tb_001_Name);
                            }
                            break;

                        case "DBD":
                            Main_Mon_Read_no_int(DBNum_001, Buffer_num001, DB_Pos_001, ref Proba_double[0]);

                            if (chb_001_act.Checked == true)
                            {
                                Proba_ot[0] = string.Format("{0:0.00}", Proba_double[0]);

                                chart1.Series[0].LegendText = tb_001_Name.Text + ": " + Proba_ot[0];
                                chart1.Series[0].Points.AddXY(N, Proba_double[0]);
                                lb_001_Monit.Text = Proba_ot[0];

                                if (chb_limit.Checked == true) count_limit<double>(Proba_double[0], ref count_1_ok, ref count_1, Limit, tb_001_Name);
                            }
                            break;
                    }

                    switch (tb_002FormDB.Text)
                    {
                        case "":

                            break;

                        case "DBW":
                            Main_Mon_Read(DBNum_002, Buffer_num002, DB_Pos_002, ref Proba_int[1]);

                            if (chb_002_act.Checked == true)
                            {
                                chart1.Series[1].LegendText = tb_002_Name.Text + ": " + Proba_int[1];
                                chart1.Series[1].Points.AddXY(N, Proba_int[1]);
                                lb_002_Monit.Text = Convert.ToString(Proba_int[1]);

                                if (chb_limit.Checked == true) count_limit<int>(Proba_int[1], ref count_2_ok, ref count_2, Limit, tb_002_Name);
                            }
                            break;

                        case "DBD":
                            Main_Mon_Read_no_int(DBNum_002, Buffer_num002, DB_Pos_002, ref Proba_double[1]);

                            if (chb_002_act.Checked == true)
                            {
                                Proba_ot[1] = string.Format("{0:0.00}", Proba_double[1]);

                                chart1.Series[1].LegendText = tb_002_Name.Text + ": " + Proba_ot[1];
                                chart1.Series[1].Points.AddXY(N, Proba_double[1]);
                                lb_002_Monit.Text = Proba_ot[1];

                                if (chb_limit.Checked == true) count_limit<double>(Proba_double[1], ref count_2_ok, ref count_2, Limit, tb_002_Name);
                            }
                            break;
                    }

                    if (chb_limit.Checked == true) out_put_limit();
                }
                catch (Exception eror)
                {
                    lb_info.Text = "Ошибка чтения данных";
                    Reader_Time.Stop();
                    btn_stop.Visible = false;
                    btn_start.Visible = true;
                    Reader_Time.Enabled = false;
                    locker_tb = true;
                    Metod_tb_lock();
                    btn_clean_chart.Enabled = true;
                    MessageBox.Show(Convert.ToString(eror));
                }
                //----------------------------------------------
            }
            else
            {
                // Эмитация чтения данных
                Saw_Control_act_1++;     
                if (Saw_Control_act_1 > 20) Saw_Control_act_2 += 1;

                Alg_sawer(ref Saw_Control_act_1);
                Alg_sawer(ref Saw_Control_act_2);
                //---------------------------------

                count_main_l(ref Saw_Control_act_1, ref count_1_ok, ref count_1, Limit);
                count_main_l(ref Saw_Control_act_2, ref count_2_ok, ref count_2, Limit);
                //---------------------------------
                lb_count_1.Text = Convert.ToString(count_2 + count_1);

                chart1.Series[0].LegendText = tb_001_Name.Text + ": " + Convert.ToString(Saw_Control_act_1);
                chart1.Series[0].Points.AddXY(N, Saw_Control_act_1);
                lb_001_Monit.Text = Convert.ToString(Saw_Control_act_1);
              
                chart1.Series[1].LegendText = tb_002_Name.Text + ": " + Convert.ToString(Saw_Control_act_2);
                chart1.Series[1].Points.AddXY(N, Saw_Control_act_2);
                lb_002_Monit.Text = Convert.ToString(Saw_Control_act_2);

                chart1.Series[2].LegendText = tb_limit_name.Text + ": " + lb_count_1.Text;
            }
           
            if (chb_limit.Checked == true)
            {
                chart1.Series[2].Points.AddXY(N, Limit);
            }
            else
            {
                chart1.Series[2].Points.Clear();
            }   

            if(X == 900)
            {
                N = 200;
                chart1.Series[0].Points.Clear();
                chart1.Series[1].Points.Clear();
                chart1.Series[2].Points.Clear();
                X = 0;
            }
        }
        // для работы с интерфэйсом
        private void Interface_Time_Tick(object sender, EventArgs e)
        {
            time++;

            if (time == 2)
            {
                time = 0;
                Interface_Time.Stop();
            }

            if (time == 0)
            {
                Interface_Time.Start();
                if (Connect_disconect == true)
                {
                    if (No_sleep == false)
                    {
                        Thread.Sleep(1000);
                        No_sleep = true;
                    }       

                    CPU_Status();
                }         
            }
            if (chb_option_Test_OnOff.Checked == false) Main_locker_btn();
            _locker_btn_creat_preset();
        }
        #endregion

        #region Статика

        #region [Не отсартированые события]
        // Скорость чтения
        private void hScrollBar1_Scroll(object sender, ScrollEventArgs e)
        {
            Reader_Time.Interval = scb_timer.Value;
            if (scb_timer.Value < 1000)
            {
                lb_inter_time.Text = "Интервал чтения:" + Convert.ToString(scb_timer.Value) + "ms.";
            }
            else
            {
                int s  = scb_timer.Value/1000;
                int ms = scb_timer.Value - 1000;

                lb_inter_time.Text = "Интервал чтения:" + Convert.ToString(s) + "s, " + Convert.ToString(ms) + "ms.";
            }
        }
        // отоброжение правой панели
        private void btn_menu_Click(object sender, EventArgs e)
        {
            visual_panel(pn_right);
        }
        // отоброжение левой панели
        private void button2_Click(object sender, EventArgs e)
        {
            visual_panel(pn_left);
        }

        #endregion

        #region [Обработчики элемента chart]
        private void chart1_MouseEnter(object sender, EventArgs e)
        {
            if (!chart1.Focused)
                chart1.Focus();
        }

        private void chart1_MouseLeave(object sender, EventArgs e)
        {
            if (chart1.Focused)
                chart1.Parent.Focus();
        }

        private void chart1_MouseMove(object sender, MouseEventArgs e)
        {
            Point pos = e.Location;
            if (prevPosition.HasValue && pos == prevPosition.Value)
                return;
            tooltip.RemoveAll();
            prevPosition = pos;
            HitTestResult[] results = chart1.HitTest(pos.X, pos.Y, false, ChartElementType.DataPoint);
            foreach (HitTestResult result in results)
            {
                if (result.ChartElementType == ChartElementType.DataPoint)
                {
                    DataPoint prop = result.Object as DataPoint;
                    if (prop != null)
                    {
                        double pointXPixel = result.ChartArea.AxisX.ValueToPixelPosition(prop.XValue);
                        double pointYPixel = result.ChartArea.AxisY.ValueToPixelPosition(prop.YValues[0]);
                        tooltip.Show("X=" + prop.XValue + ", Y=" + prop.YValues[0], this.chart1, pos.X + 10, pos.Y + 15); // Позиция всплывающего окна (От курсора)
                    }
                }
            }
        }
        #endregion

        #region [Обработка текст-бокса]
        // Коректировка формата числа (DBD/DBW) tb_001FormDB
        private void tb_001FormDB_TextChanged(object sender, EventArgs e)
        {
            corect_format(tb_001FormDB);
        }
        // Коректировка формата числа (DBD/DBW) tb_002FormDB
        private void tb_002FormDB_TextChanged(object sender, EventArgs e)
        {
            corect_format(tb_002FormDB);
        }
        // Для блокировк кнопок установк Лимита, если нет значения для лимита
        private void tb_limit_zn_TextChanged(object sender, EventArgs e)
        {
            if (tb_limit_zn.Text == "")
            {
                chb_limit.Enabled = false;
                btn_set_limit.Enabled = false;
            }
            else
            {
                chb_limit.Enabled = true;
                btn_set_limit.Enabled = true;
            }
        }
        #endregion

        #region [Обработка кнопок]
        // Установка лимита
        private void btn_set_limit_Click(object sender, EventArgs e)
        {
            chart1.Series[2].LegendText = tb_limit_name.Text;

            if (chb_limit.Checked == true)
            {
                chart1.Series[2].IsVisibleInLegend = true;
            }
            else
            {
                chart1.Series[2].IsVisibleInLegend = false;
            }

            Limit = Convert.ToInt32(tb_limit_zn.Text);
        }
        // Установка иследуймых данных
        private void btn_set_reset_Click(object sender, EventArgs e)
        {
            //Установка связи
            IP = Convert.ToString(tb_ip.Text);
            RACK = Convert.ToInt32(tb_rack.Text);
            SLOT = Convert.ToInt32(tb_slot.Text);

            //Работка с данными 1
            if (chb_001_act.Checked == true)
            {
                chart1.Series[0].LegendText = tb_001_Name.Text;

                num_size_001 = Convert.ToInt32(tb_001_sizeDB.Text);
                Buffer_num001 = new byte[num_size_001];

                DBNum_001 = Convert.ToInt32(tb_001NumDB.Text);

                DB_Pos_001 = Convert.ToInt32(tb_001PosDB.Text);
            }

            //Работка с данными 2
            if (chb_002_act.Checked == true)
            {
                chart1.Series[1].LegendText = tb_002_Name.Text;

                num_size_002 = Convert.ToInt32(tb_002_sizeDB.Text);
                Buffer_num002 = new byte[num_size_002];

                DBNum_002 = Convert.ToInt32(tb_002NumDB.Text);

                DB_Pos_002 = Convert.ToInt32(tb_002PosDB.Text);
            }

            if (Connect_disconect == true)
            {
                MessageBox.Show("Установка данных прошла успешно!");
            }
            else
            {
                MessageBox.Show("Установка данных прошла успешно. Но вы не подключились, к контролеру...");
                lb_info.Text = "Заполните блок связи (В верхнем правом углу) и нажмите кнопку 'Подкл.'";
            }
            
        }
        // Связь
        private void btn_Connect_Click_1(object sender, EventArgs e)
        {
            No_sleep = false;

            if (Connect_disconect == false)
            {
                IP = Convert.ToString(tb_ip.Text);
                RACK = Convert.ToInt32(tb_rack.Text);
                SLOT = Convert.ToInt32(tb_slot.Text);

                Result = Client.ConnectTo(IP, RACK, SLOT);
                ShowResult(Result);

                if (Result == 0)
                {         
                    Connect_disconect = true;
                    btn_Connect.Text = "Откл.";
                    tabControl1.Enabled = true;
                    btn_start.Enabled = true;
                    btn_prset_set.Enabled = true;
                }
            }
            else
            {
                Connect_disconect = false;
                Client.Disconnect();
                btn_Connect.Text = "Подкл.";
                lb_info.Text = "Вы отключились от контролера.";
                tabControl1.Enabled = false;
                pn_Top_Left_led_onlin.BackColor = System.Drawing.Color.DarkGray;
            }           
        }
        // Старт
        private void btn_start_Click(object sender, EventArgs e)
        {
            Start_();
        }
        // Стоп
        private void btn_stop_Click(object sender, EventArgs e)
        {
            Stop_();
        }
        // Сброс счётчика привышения лимита
        private void btn_count_rest_Click(object sender, EventArgs e)
        {
            count_1 = 0;
            count_2 = 0;
            count_all = 0;
            lb_count_1.Text = "0";
        }
        // Очистка графика
        private void btn_clean_chart_Click(object sender, EventArgs e)
        {
            chart1.Series[0].Points.Clear();
            chart1.Series[1].Points.Clear();
            chart1.Series[2].Points.Clear();
            btn_clean_chart.Enabled = false;
        }
        // Поиск пресетов
        private void btn_prset_set_Click(object sender, EventArgs e)
        {
            MainPreset.Filter = "MPS|*.mps";

            if (MainPreset.ShowDialog() == DialogResult.OK)
            {
                _set_pres = MainPreset.FileName;
                Reader_preset();
            }
        }
        // Очистка текстбоксов первого числа
        private void btn_001_clean_Click(object sender, EventArgs e)
        {
            Cleaner_tb(tb_001FormDB, tb_001NumDB, tb_001PosDB, tb_001_Name, tb_001_sizeDB);
        }
        // Очистка текстбоксов второго числа
        private void btn_002_clean_Click(object sender, EventArgs e)
        {
            Cleaner_tb(tb_002FormDB, tb_002NumDB, tb_002PosDB, tb_002_Name, tb_002_sizeDB);
        }
        // Копирование текста из раздела "1.Число" в "2.Число"
        private void btn_001_copy_Click(object sender, EventArgs e)
        {
            copy_tb(tb_001FormDB, tb_001NumDB, tb_001PosDB, tb_001_Name, tb_001_sizeDB, tb_002FormDB, tb_002NumDB, tb_002PosDB, tb_002_Name, tb_002_sizeDB);
        }
        // Копирование текста из раздела "2.Число" в "1.Число"
        private void btn_002_copy_Click(object sender, EventArgs e)
        {
            copy_tb(tb_002FormDB, tb_002NumDB, tb_002PosDB, tb_002_Name, tb_002_sizeDB, tb_001FormDB, tb_001NumDB, tb_001PosDB, tb_001_Name, tb_001_sizeDB);
        }
        // Для установки пресетов
        private void btn_preset_creat_Click(object sender, EventArgs e)
        {
            MainSavePreset.OverwritePrompt = true;
            MainSavePreset.CheckPathExists = true;
            try
            {
                if (tb_001_Name.Text == "" || tb_001NumDB.Text == "" || tb_001_sizeDB.Text == "" || tb_001PosDB.Text == "" || tb_001FormDB.Text == "" || tb_002_Name.Text == "" || tb_002NumDB.Text == "" || tb_002_sizeDB.Text == "" || tb_002PosDB.Text == "" || tb_002FormDB.Text == "")
                {
                    MessageBox.Show("У вас есть не за полненные поля! Сохранения параметров не возможно...");
                    lb_info.Text = "";
                }
                else
                {

                    string Preset = "1:" + tb_001_Name.Text + "::" + tb_001NumDB.Text + ":::" + tb_001FormDB.Text + "::::" + tb_001PosDB.Text + ":P:" + tb_001_sizeDB.Text + ".2:" + tb_002_Name.Text + "::1:::" + tb_002FormDB.Text + "::::" + tb_002PosDB.Text + ":P:" + tb_002_sizeDB.Text + ".IP:" + tb_ip.Text + "XRack:" + tb_rack.Text + "XXSlot:" + tb_slot.Text;

                    MainSavePreset.Filter = "MPS|*.mps";
                    if (MainSavePreset.ShowDialog() == DialogResult.OK)
                    {
                        using (Stream s = File.Open(MainSavePreset.FileName, FileMode.CreateNew))
                        using (StreamWriter sw = new StreamWriter(s))
                        {
                            sw.Write(Preset);
                        }
                    }
                }
            }
            catch
            {
                string Preset = "1:" + tb_001_Name.Text + "::" + tb_001NumDB.Text + ":::" + tb_001FormDB.Text + "::::" + tb_001PosDB.Text + ":P:" + tb_001_sizeDB.Text + ".2:" + tb_002_Name.Text + "::1:::" + tb_002FormDB.Text + "::::" + tb_002PosDB.Text + ":P:" + tb_002_sizeDB.Text + ".IP:" + tb_ip.Text + "XRack:" + tb_rack.Text + "XXSlot:" + tb_slot.Text;

                MainSavePreset.Filter = "MPS|*.mps";
                if (MainSavePreset.ShowDialog() == DialogResult.OK)
                {
                    using (Stream s = File.Open(MainSavePreset.FileName, FileMode.CreateNew))
                    using (StreamWriter sw = new StreamWriter(s))
                    {
                        sw.Write(Preset);
                    }
                }
                lb_info.Text = "Попробуйте сохранить пресет, под другим иминем файла.";
            }
        }
        // Выбор цвета для первого числа
        private void btn_option_color_ser1_Click(object sender, EventArgs e)
        {
            DialogResult colorResult = colorDialog1.ShowDialog();
            if (colorResult == DialogResult.OK)
            {
                chart1.Series[0].Color = colorDialog1.Color;
                MessageBox.Show("Установка цвета графика для: 1.Чила. Завершина...");
            }
        }
        // Выбор цвета для второго числа
        private void btn_option_color_ser2_Click(object sender, EventArgs e)
        {
            DialogResult colorResult = colorDialog1.ShowDialog();
            if (colorResult == DialogResult.OK)
            {
                chart1.Series[1].Color = colorDialog1.Color;
                MessageBox.Show("Установка цвета графика для: 2.Чила. Завершина...");
            }
        }
        // Кнопка Скриншот
        private void btn_screenshot_Click(object sender, EventArgs e)
        {
            if (btn_start.Visible == false)
            {
                Stop_();
                Screen_shot_();
                Start_();
            }
            else
            {
                Screen_shot_();
            }
        }

        #endregion

        #region [Обработка Чек-боксовов]
        // Отображения графика для первого числа(1)
        private void chb_001_act_CheckedChanged(object sender, EventArgs e)
        {
            //отоброжения данных 1
            if (chb_001_act.Checked == true)
            {
                chart1.Series[0].IsVisibleInLegend = true;
                chart1.Series[0].LegendText = tb_001_Name.Text;
            }
            else
            {
                chart1.Series[0].IsVisibleInLegend = false;
            }

        }
        // Отоброжения точек со значениями для первого числа(1)
        private void chb_seris_1_CheckedChanged(object sender, EventArgs e)
        {
            if (chart1.Series[0].IsValueShownAsLabel == false)
            {
                chart1.Series[0].IsValueShownAsLabel = true;
           
                chart1.Series[0].MarkerStyle = System.Windows.Forms.DataVisualization.Charting.MarkerStyle.Square;
                chart1.Series[0].MarkerBorderColor = System.Drawing.Color.DimGray;
            }
            else
            {
                chart1.Series[0].IsValueShownAsLabel = false;
                chart1.Series[0].MarkerStyle = System.Windows.Forms.DataVisualization.Charting.MarkerStyle.None;
            }
        }
        // Отображения графика для второго числа(2)
        private void chb_002_act_CheckedChanged(object sender, EventArgs e)
        {
            //отоброжения данных 2
            if (chb_002_act.Checked == true)
            {
                chart1.Series[1].IsVisibleInLegend = true;
                chart1.Series[1].LegendText = tb_002_Name.Text;
            }
            else
            {
                chart1.Series[1].IsVisibleInLegend = false;
            }
        }
        // Отоброжения точек со значениями для вторго числа(2)
        private void chb_seris_2_CheckedChanged(object sender, EventArgs e)
        {
            if (chart1.Series[1].IsValueShownAsLabel == false)
            {
                chart1.Series[1].IsValueShownAsLabel = true;
                chart1.Series[1].MarkerStyle = System.Windows.Forms.DataVisualization.Charting.MarkerStyle.Square;
                chart1.Series[1].MarkerBorderColor = System.Drawing.Color.Blue;
            }
            else
            {
                chart1.Series[1].IsValueShownAsLabel = false;
                chart1.Series[1].MarkerStyle = System.Windows.Forms.DataVisualization.Charting.MarkerStyle.None;
            }
        }
        // Обработка активации тестового режима
        private void chb_option_Test_OnOff_CheckedChanged(object sender, EventArgs e)
        {
            if (chb_option_Test_OnOff.Checked == true)
            {
                btn_Connect.Enabled = false;
                btn_set_reset.Enabled = false;
                tabControl1.Enabled = true;
                chb_option_Test_OnOff.Text = "Вкл!";
                lb_info.Text = "Вы запустили тестовый режим.";

                tb_001_Name.Text = "Saw_Num1_Control_[A]";
                tb_001NumDB.Text = "1";
                tb_001FormDB.Text = "DBW";
                tb_001PosDB.Text = "0";
                tb_001_sizeDB.Text = "4";
                chb_001_act.Checked = true;

                tb_002_Name.Text = "Saw_Num2_Control_[A]";
                tb_002NumDB.Text = "1";
                tb_002FormDB.Text = "DBW";
                tb_002PosDB.Text = "4";
                tb_002_sizeDB.Text = "4";
                chb_002_act.Checked = true;
                btn_start.Enabled = true;
                btn_prset_set.Enabled = true;
            }
            else
            {
                Saw_Control_act_1 = 0;
                Saw_Control_act_2 = 0;

                btn_Connect.Enabled = true;
                btn_set_reset.Enabled = false;
                tabControl1.Enabled = false;
                chb_option_Test_OnOff.Text = "Выкл...";
                lb_info.Text = "Вы вышле из тестового режима...";

                tb_001_Name.Text = "";
                tb_001NumDB.Text = "";
                tb_001FormDB.Text = "";
                tb_001PosDB.Text = "";
                tb_001_sizeDB.Text = "";
                chb_001_act.Checked = false;
                lb_001_Monit.Text = "0.0";
                tb_002_Name.Text = "";
                tb_002NumDB.Text = "";
                tb_002FormDB.Text = "";
                tb_002PosDB.Text = "";
                tb_002_sizeDB.Text = "";
                chb_002_act.Checked = false;
                lb_002_Monit.Text = "0.0";
                btn_start.Enabled = false;
                btn_prset_set.Enabled = false;
            }
        }
        // Для автоматического отчёта по превышению лимита
        private void chb_auto_otch_CheckedChanged(object sender, EventArgs e)
        {
            if (chb_auto_otch.Checked == true)
            {
                string data_in = "Дата отчёта: " + DateTime.Now.ToShortDateString() + "; Время - " + DateTime.Now.ToLongTimeString() + ");" + "\r\n";

                try
                {
                    SaveFileDialog saf = new SaveFileDialog();
                    saf.Title = "Сохранить отчёт, как...";
                    saf.OverwritePrompt = true;
                    saf.CheckPathExists = true;
                    saf.Filter = "TXT|*.txt";
                    if (saf.ShowDialog() == DialogResult.OK)
                    {
                        using (Stream s = File.Open(saf.FileName, FileMode.CreateNew))
                        using (StreamWriter sw = new StreamWriter(s))
                        {
                            Limit_path = saf.FileName;
                            sw.Write(data_in);
                        }
                    }
                }
                catch
                {
                    lb_info.Text = "Присвойте новое имя отчёту";
                    MessageBox.Show("Файл не может быть, перезаписан!!!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        // Блокировка автомотического отчёта
        private void chb_limit_CheckedChanged(object sender, EventArgs e)
        {
            if (chb_limit.Checked == true)
            {
                chb_auto_otch.Enabled = true;
            }
            else
            {
                chb_auto_otch.Enabled = false;
            }
        }
        #endregion

        #region [Блокировка симвлолов ввода текстбоксов]
        //Изключения клавиш для текст бокса "IP"
        private void For_IP_KeyPress(object sender, KeyPressEventArgs e)
        {
            if ((e.KeyChar >= '0') && (e.KeyChar <= '9') || (e.KeyChar == '.') || (e.KeyChar == (char)Keys.Back))
            {
                return;
            }
            else
            {
                e.Handled = true;
            }
        }
        //Изключения клавиш для текст бокса, в которых пременяются только цифры
        private void Only_Num_KeyPress(object sender, KeyPressEventArgs e)
        {
            if ((e.KeyChar >= '0') && (e.KeyChar <= '9') || (e.KeyChar == (char)Keys.Back))
            {
                return;
            }
            else
            {
                e.Handled = true;
            }
        }
        //Изключения клавиш для текст бокса "Формат числа"
        private void For_Format_tb_KyePress(object sender, KeyPressEventArgs e)
        {
            if ((e.KeyChar == 'd') || (e.KeyChar == 'b') || (e.KeyChar == 'w') || (e.KeyChar == 'D') || (e.KeyChar == 'B') || (e.KeyChar == 'W') || (e.KeyChar == (char)Keys.Back))
            {
                return;
            }
            else
            {
                e.Handled = true;
            }
        }
        // Блокировка символов через метот Name_lock_symbols() для 1.Числа
        private void tb_001_Name_KeyPress(object sender, KeyPressEventArgs e)
        {
            Name_lock_symbols(e);
        }
        // Блокировка символов через метот Name_lock_symbols() для 2.Числа
        private void tb_002_Name_KeyPress(object sender, KeyPressEventArgs e)
        {
            Name_lock_symbols(e);
        }
        // Коректировка символов для 1.Числа
        private void tb_001_Name_TextChanged(object sender, EventArgs e)
        {
            Name_corect(tb_001_Name);
        }
        // Коректировка символов для 2.Числа
        private void tb_002_Name_TextChanged(object sender, EventArgs e)
        {
            Name_corect(tb_002_Name);
        }
        #endregion

        #region [Для очистки текст боксов из блока связи]
        private void mous_clean_tb_block_connect_IP(object sender, MouseEventArgs e)
        {
            clean_connect(tb_ip);
        }

        private void mous_clean_tb_block_connect_Rack(object sender, MouseEventArgs e)
        {
            clean_connect(tb_rack);
        }

        private void mous_clean_tb_block_connect_Slot(object sender, MouseEventArgs e)
        {
            clean_connect(tb_slot);
        }
        #endregion

        #region [Обработчик элементов комбо-бокса]
        // Толщина линии графика и лимита
        private void cb_option_Diogram_line_size_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (cb_option_Diogram_line_size.SelectedIndex)
            {
                case 0:
                    chart1.Series[0].BorderWidth = 3;
                    chart1.Series[1].BorderWidth = 3;
                    chart1.Series[2].BorderWidth = 3;
                    break;

                case 1:
                    chart1.Series[0].BorderWidth = 4;
                    chart1.Series[1].BorderWidth = 4;
                    chart1.Series[2].BorderWidth = 4;
                    break;

                case 2:
                    chart1.Series[0].BorderWidth = 5;
                    chart1.Series[1].BorderWidth = 5;
                    chart1.Series[2].BorderWidth = 5;
                    break;

                case 3:
                    chart1.Series[0].BorderWidth = 6;
                    chart1.Series[1].BorderWidth = 6;
                    chart1.Series[2].BorderWidth = 6;
                    break;

                case 4:
                    chart1.Series[0].BorderWidth = 7;
                    chart1.Series[1].BorderWidth = 7;
                    chart1.Series[2].BorderWidth = 7;
                    break;

                case 5:
                    chart1.Series[0].BorderWidth = 8;
                    chart1.Series[1].BorderWidth = 8;
                    chart1.Series[2].BorderWidth = 8;
                    break;

                case 6:
                    chart1.Series[0].BorderWidth = 9;
                    chart1.Series[1].BorderWidth = 9;
                    chart1.Series[2].BorderWidth = 9;
                    break;

                case 7:
                    chart1.Series[0].BorderWidth = 10;
                    chart1.Series[1].BorderWidth = 10;
                    chart1.Series[2].BorderWidth = 10;
                    break;
            }

        }
        // Вид графиков
        private void cb_option_Diogram_form_prise_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (cb_option_Diogram_form_prise.SelectedIndex)
            {
                case 0:
                    chart1.Series[0].ChartArea = "ChartArea1";
                    chart1.Series[0].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Area;

                    chart1.Series[1].ChartArea = "ChartArea1";
                    chart1.Series[1].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Area;

                    cb_option_Diogram_line_size.Enabled = false;
                    break;

                case 1:
                    chart1.Series[0].ChartArea = "ChartArea1";
                    chart1.Series[0].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;

                    chart1.Series[1].ChartArea = "ChartArea1";
                    chart1.Series[1].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;

                    cb_option_Diogram_line_size.Enabled = true;
                    break;

                default:
                    cb_option_Diogram_line_size.Enabled = false;
                    break;
            }
        }
        // Блокировка ввода клавиатуры в комбо-бокс
        private void cb_option_Diogram_form_prise_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
        }
        // Назначения стандартного значения видов графика
        private void cb_option_Diogram_form_prise_TextChanged(object sender, EventArgs e)
        {
            switch (cb_option_Diogram_form_prise.Text)
            {
                case "1.Area all":

                    break;

                case "2.Line all":

                    break;

                default:
                    cb_option_Diogram_form_prise.Text = "1.Area all";
                    break;
            }
        }
        #endregion

        #endregion
        
    }
}