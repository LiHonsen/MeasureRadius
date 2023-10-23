using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using HalconDotNet;

namespace _05_测量半径
{
	
	public partial class FrmMain : Form
	{
		
		public FrmMain()
		{
			this.InitializeComponent();
		}
		// 窗口句柄
		private HTuple hv_WindowHandle;

		// 图片数组
		private HObject[] hImageArrary = new HObject[8];

		//图片
		private HObject ho_Image;

		// 线程
		private Thread t1;

		// 线程运行标志位
		private bool bThreadStatus = false;
		//窗体加载事件
		private void FrmMain_Load(object sender, EventArgs e)
		{
			this.DisplayHWindow();
			this.LoadImage();
			this.btnStop.Enabled = false;
		}

		//开始
		private void btnStart_Click(object sender, EventArgs e)
		{
			this.btnStart.Enabled = false;
			this.btnStop.Enabled = true;
			//启动线程运行
			this.t1 = new Thread(new ThreadStart(this.Run));
			this.t1.IsBackground = true;
			this.t1.Start();
		}

		// 停止
		private void btnStop_Click(object sender, EventArgs e)
		{
			this.btnStart.Enabled = true;
			this.btnStop.Enabled = false;
			this.bThreadStatus = false;
			this.t1.Abort();
		}

		// 关闭
		private void btnExit_Click(object sender, EventArgs e)
		{
			base.Close();
		}

		// 执行halcon算法
		private void Run()
		{
			int i = 0;
			this.bThreadStatus = true;
			while (this.bThreadStatus)
			{
				// 获取图片
				this.ho_Image = this.hImageArrary[i];
				// 卡尺搜索长度
				HTuple hv_CircleTolerance = 80;
				// 预期圆半径
				HTuple hv_CircleExpectedRadius = 320;
				HTuple hv_Width;
				HTuple hv_Height;
				// 获取图像大小
				HOperatorSet.GetImageSize(this.ho_Image, out hv_Width, out hv_Height);

				// 二值化处理
				HObject ho_Region;
				HOperatorSet.Threshold(this.ho_Image, out ho_Region, 0, 60);
				// 连通域
				HObject ho_ResultConnectedRegions;
				HOperatorSet.Connection(ho_Region, out ho_ResultConnectedRegions);
				// 筛选面积
				HObject ho_ResultSelectedRegions;
				HOperatorSet.SelectShape(ho_ResultConnectedRegions, out ho_ResultSelectedRegions, new HTuple("area").TupleConcat("roundness"), "and", new HTuple(250000).TupleConcat(0.9), new HTuple(320000).TupleConcat(1));
				//求中心点坐标
				HTuple hv_ResultArea;
				HTuple hv_ResultRow;
				HTuple hv_ResultColumn;
				HOperatorSet.AreaCenter(ho_ResultSelectedRegions, out hv_ResultArea, out hv_ResultRow, out hv_ResultColumn);
				bool flag = HDevWindowStack.IsOpen();
				if (flag)
					HOperatorSet.SetColor(HDevWindowStack.GetActive(), "green");
				bool flag2 = HDevWindowStack.IsOpen();
				if (flag2)
					HOperatorSet.SetDraw(HDevWindowStack.GetActive(), "margin");
				bool flag3 = HDevWindowStack.IsOpen();
				if (flag3)
					HOperatorSet.DispObj(this.ho_Image, HDevWindowStack.GetActive());
				//生成圆
				HObject ho_Circle;
				HOperatorSet.GenCircle(out ho_Circle, hv_ResultRow, hv_ResultColumn, hv_CircleExpectedRadius);
				//创建测量句柄
				HTuple hv_MetrologyHandle;
				HOperatorSet.CreateMetrologyModel(out hv_MetrologyHandle);
				//设置测量句柄的图像大小
				HOperatorSet.SetMetrologyModelImageSize(hv_MetrologyHandle, hv_Width, hv_Height);
				//添加圆
				HTuple hv_Index;
				HOperatorSet.AddMetrologyObjectCircleMeasure(hv_MetrologyHandle, hv_ResultRow, hv_ResultColumn, hv_CircleExpectedRadius, hv_CircleTolerance, 5, 1.5, 30, new HTuple("measure_transition").TupleConcat("min_score"), new HTuple("negative").TupleConcat(0.4), out hv_Index);

				//获取卡尺区域
				HObject ho_Calipers;
				HTuple hv_Row;
				HTuple hv_Column;
				HOperatorSet.GetMetrologyObjectMeasures(out ho_Calipers, hv_MetrologyHandle, "all", "all", out hv_Row, out hv_Column);
				//测量
				HOperatorSet.ApplyMetrologyModel(this.ho_Image, hv_MetrologyHandle);
				//获取结果轮廓
				HObject ho_Contour;
				HOperatorSet.GetMetrologyObjectResultContour(out ho_Contour, hv_MetrologyHandle, "all", "all", 1.5);
				//获取结果
				HTuple hv_CircleRadius;
				HOperatorSet.GetMetrologyObjectResult(hv_MetrologyHandle, hv_Index, "all", "result_type", "radius", out hv_CircleRadius);
				//适合图形
				HOperatorSet.SetPart(this.hv_WindowHandle, 0, 0, hv_Height, hv_Width);
				//显示图片
				HOperatorSet.DispObj(this.ho_Image, this.hv_WindowHandle);
				//显示圆
				HOperatorSet.DispObj(ho_Contour, this.hv_WindowHandle);
				//设置字符串显示位置
				HOperatorSet.SetTposition(this.hv_WindowHandle, 100, 100);
				//打印字符串
				HOperatorSet.WriteString(this.hv_WindowHandle, "圆半径：" + hv_CircleRadius);
				//清除测量句柄
				HOperatorSet.ClearMetrologyModel(hv_MetrologyHandle);
				//结果
				string strResult = (hv_CircleRadius >= 311 && hv_CircleRadius <= 313) ? "PASS" : "FAIL";
				//结果以","连接
				string result = string.Format("{0},{1},{2}", i, hv_CircleRadius.D.ToString("0.000"), strResult);
				//更新listview控件
				this.UpdateListViewHandle(result);
				Thread.Sleep(200);
				i++;
				
				if (i >= 8) i = 0;
				
			}
		}

		// 保存图片
		private void WriteImage(HObject hImage)
		{
			string FileName = DateTime.Now.ToString("yyyy年MM月dd日HH时mm分ss秒fff毫秒");
			HOperatorSet.WriteImage(hImage, "bmp", 0, "图片/" + FileName + ".bmp");
		}

		// 更新控件
		private void UpdateListView(string result)
		{
			//分割字符串
			string[] SubResult = result.Split(',');

			//创建listview子项
			ListViewItem lt = new ListViewItem(SubResult);
			//添加
			this.listView1.Items.Add(lt);
			while (this.listView1.Items.Count > 20)
			{
				this.listView1.Items.RemoveAt(0);
			}
		}

		//委托调用更新控件显示
		private void UpdateListViewHandle(string result)
		{
			base.Invoke(new MethodInvoker(delegate()
			{
				this.UpdateListView(result);
			}));
		}

		// 设置halcon窗口
		private void DisplayHWindow()
		{
			HOperatorSet.SetWindowAttr("background_color", "black");
			HOperatorSet.OpenWindow(0, 0, this.hWindowControl1.Width, this.hWindowControl1.Height, this.hWindowControl1.HalconWindow, "visible", "", out this.hv_WindowHandle);
			HDevWindowStack.Push(this.hv_WindowHandle);
		}

		// 加载图片
		private void LoadImage()
		{
			for (int i = 0; i < this.hImageArrary.Length; i++)
			{
				HOperatorSet.ReadImage(out this.hImageArrary[i], string.Format("图片\\{0}.bmp", i));
			}
		}

		
	}
}
