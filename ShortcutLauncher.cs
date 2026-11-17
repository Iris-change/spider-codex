using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Threading;

public class SilkLauncher : Window
{
    [StructLayout(LayoutKind.Sequential)] public struct NativePoint { public int X,Y; }
    [DllImport("user32.dll")] static extern bool GetCursorPos(out NativePoint p);
    [DllImport("user32.dll")] static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] static extern short GetAsyncKeyState(int key);
    public class Frame { public string file; public int x,y,width,height; }
    readonly Canvas stage=new Canvas { Width=1280,Height=720,IsHitTestVisible=false };
    readonly Image image=new Image();
    readonly Button workspace=new Button();
    readonly DrawingVisual drawing=new DrawingVisual();
    readonly Stopwatch clock=new Stopwatch();
    readonly Point origin;
    readonly System.Windows.Threading.DispatcherTimer escapeTimer=new System.Windows.Threading.DispatcherTimer();
    BitmapSource[] bitmaps;
    Frame[] frames;
    int last=-1;
    bool closed;
    readonly string root=AppDomain.CurrentDomain.BaseDirectory;
    class VisualHost : FrameworkElement { readonly Visual child; public VisualHost(Visual v){child=v;AddVisualChild(v);} protected override int VisualChildrenCount{get{return 1;}} protected override Visual GetVisualChild(int i){return child;} }
    public static Point Map(double x,double y,double width,double height)
    { double s=Math.Min(width/1280,height/720);return new Point((x-(width-1280*s)/2)/s,(y-(height-720*s)/2)/s); }
    [STAThread] public static void Main(string[] args)
    {
        SetProcessDPIAware();NativePoint p;GetCursorPos(out p);
        try {
            if(args.Length>0 && args[0]=="--selftest") {
                Point a=Map(960,540,1920,1080),b=Map(0,0,1920,1080),c=Map(1280,720,2560,1440);
                if(a.X!=640||a.Y!=360||b.X!=0||b.Y!=0||c.X!=640||c.Y!=360)throw new Exception("Coordinate check failed");
                string folder=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"detail-transparent-frames");
                var list=new JavaScriptSerializer().Deserialize<Frame[]>(File.ReadAllText(Path.Combine(folder,"manifest.json")));
                if(list.Length!=360)throw new Exception("Missing frames");
                foreach(var f in list)if(!File.Exists(Path.Combine(folder,f.file)))throw new Exception("Missing frame "+f.file);
                File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"shortcut-check.txt"),"PASS: coordinate mapping; 360 animation frames present");return;
            }
            new Application().Run(new SilkLauncher(p,Array.IndexOf(args,"--record-demo")>=0));
        } catch(Exception e){MessageBox.Show(e.Message,"蛛丝启动器");}
    }
    SilkLauncher(NativePoint p,bool recordDemo)
    {
        var monitor=System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point(p.X,p.Y)).Bounds;
        double dpi;using(var g=System.Drawing.Graphics.FromHwnd(IntPtr.Zero)){dpi=g.DpiX/96.0;}
        // Keep the overlay strictly smaller than both the display and work area:
        // wallpaper players otherwise classify a transparent window as fullscreen.
        var area=System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point(p.X,p.Y)).WorkingArea;
        const int inset=12;
        origin=Map(p.X-area.Left-inset,p.Y-area.Top-inset,area.Width-2*inset,area.Height-2*inset);
        Title="Spider shortcut launcher";WindowStyle=WindowStyle.None;AllowsTransparency=true;Background=Brushes.Transparent;ResizeMode=ResizeMode.NoResize;Topmost=true;ShowActivated=false;ShowInTaskbar=recordDemo;
        Left=(area.Left+inset)/dpi;Top=(area.Top+inset)/dpi;Width=(area.Width-2*inset)/dpi;Height=(area.Height-2*inset)/dpi;
        SourceInitialized+=(s,e)=>System.Windows.Interop.HwndSource.FromHwnd(new System.Windows.Interop.WindowInteropHelper(this).Handle).AddHook(NoActivate);
        var grid=new Grid();Content=grid;var view=new Viewbox{Stretch=Stretch.Uniform,Child=stage};grid.Children.Add(view);
        stage.Children.Add(image);stage.Children.Add(new VisualHost(drawing));
        var close=new Button{Content="×",ToolTip="关闭 (Esc)",FontSize=27,Width=42,Height=42,Margin=new Thickness(0,12,14,0),HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Top,Foreground=Brushes.White};
        System.Windows.Automation.AutomationProperties.SetName(close,"关闭");
        var hitArea=new FrameworkElementFactory(typeof(Border));
        hitArea.SetValue(Border.BackgroundProperty,Brushes.Transparent);
        var glyph=new FrameworkElementFactory(typeof(ContentPresenter));
        glyph.SetValue(HorizontalAlignmentProperty,HorizontalAlignment.Center);
        glyph.SetValue(VerticalAlignmentProperty,VerticalAlignment.Center);
        hitArea.AppendChild(glyph);
        close.Template=new ControlTemplate(typeof(Button)){VisualTree=hitArea};
        close.MouseEnter+=(o,args)=>close.Foreground=Brushes.LightPink;
        close.MouseLeave+=(o,args)=>close.Foreground=Brushes.White;
        var wordmark=new StackPanel{Orientation=Orientation.Horizontal};
        var red=new TextBlock{Text="SPIDERMAN",FontFamily=new FontFamily("Impact"),FontSize=39,Padding=new Thickness(12,2,12,3),Foreground=Brushes.White,Background=new SolidColorBrush(Color.FromRgb(221,17,40))};
        var white=new TextBlock{Text="WORKSPACE",FontFamily=new FontFamily("Bahnschrift Condensed"),FontSize=34,FontWeight=FontWeights.SemiBold,Foreground=Brushes.White,Padding=new Thickness(5,0,5,1)};
        wordmark.Children.Add(red);
        wordmark.Children.Add(new Border{Child=white,Margin=new Thickness(9,0,0,0),VerticalAlignment=VerticalAlignment.Center,BorderBrush=Brushes.White,BorderThickness=new Thickness(0,2,0,2)});
        workspace.Content=wordmark;
        workspace.Background=Brushes.Transparent;
        workspace.BorderThickness=new Thickness(0);
        workspace.HorizontalAlignment=HorizontalAlignment.Center;
        workspace.VerticalAlignment=VerticalAlignment.Top;

        workspace.Cursor=System.Windows.Input.Cursors.Hand;
        workspace.Visibility=Visibility.Collapsed;
        var workspaceHit=new FrameworkElementFactory(typeof(Border));
        workspaceHit.SetValue(Border.BackgroundProperty,Brushes.Transparent);
        workspaceHit.AppendChild(new FrameworkElementFactory(typeof(ContentPresenter)));
        workspace.Template=new ControlTemplate(typeof(Button)){VisualTree=workspaceHit};
        workspace.MouseEnter+=(o,args)=>wordmark.Opacity=.78;
        workspace.MouseLeave+=(o,args)=>wordmark.Opacity=1;
        workspace.Click+=(o,args)=>OpenWorkspace();
        grid.Children.Add(workspace);
        close.Click+=(s,e)=>Close();grid.Children.Add(close);PreviewKeyDown+=(s,e)=>{if(e.Key==System.Windows.Input.Key.Escape)Close();};
        escapeTimer.Interval=TimeSpan.FromMilliseconds(30);
        escapeTimer.Tick+=(s,e)=>{if((GetAsyncKeyState(0x1B)&0x8000)!=0)Close();};
        Closed+=(s,e)=>{closed=true;escapeTimer.Stop();CompositionTarget.Rendering-=Render;clock.Stop();};
        Loaded+=(s,e)=>{
            escapeTimer.Start();
            var thread=new Thread(()=>{
                try{
                    string folder=Path.Combine(root,"detail-transparent-frames");
                    frames=new JavaScriptSerializer().Deserialize<Frame[]>(File.ReadAllText(Path.Combine(folder,"manifest.json")));
                    bitmaps=new BitmapSource[frames.Length];
                    for(int i=0;i<frames.Length;i++){
                        if(closed)return;
                        var bmp=new BitmapImage();bmp.BeginInit();bmp.CacheOption=BitmapCacheOption.OnLoad;bmp.UriSource=new Uri(Path.Combine(folder,frames[i].file));bmp.EndInit();bmp.Freeze();bitmaps[i]=bmp;
                    }
                    if(closed)return;
                    Dispatcher.BeginInvoke(new Action(()=>{if(closed)return;clock.Start();CompositionTarget.Rendering+=Render;}));
                }catch(Exception ex){if(!closed)Dispatcher.BeginInvoke(new Action(()=>{MessageBox.Show(ex.Message,"蛛丝素材加载失败");Close();}));}
            });thread.IsBackground=true;thread.Priority=ThreadPriority.BelowNormal;thread.Start();
        };
    }
    IntPtr NoActivate(IntPtr hwnd,int message,IntPtr wParam,IntPtr lParam,ref bool handled)
    {
        if(message==0x21){handled=true;return new IntPtr(3);}
        return IntPtr.Zero;
    }
    void Render(object sender,EventArgs e)
    {
        double t=clock.Elapsed.TotalSeconds;
        using(var dc=drawing.RenderOpen()) {
            if(t<.2833333333){
                double p=Math.Min(1,t/.2833333333),dx=630-origin.X,dy=299-origin.Y,len=Math.Max(1,Math.Sqrt(dx*dx+dy*dy)),nx=-dy/len,ny=dx/len;
                double travel=1-Math.Pow(1-p,1.5),length=Math.Min(len*travel,110+65*(1-p));
                Point tip=new Point(origin.X+dx*travel,origin.Y+dy*travel);
                for(int j=0;j<19;j++){
                    bool core=j<3;double offset=core?(j-1)*1.35:Math.Sin(j*2.399)*4.2;
                    var geometry=new StreamGeometry();using(var g=geometry.Open()){
                        for(int k=0;k<=44;k++){
                            double q=k/44.0,back=length*(1-q),spread=Math.Pow(1-q,.7);
                            double twist=core?.28*Math.Sin(q*13+j):Math.Sin(q*18+j*2.4)*(.55+spread*1.2);
                            double cross=(offset+twist)*spread;
                            Point pt=new Point(tip.X-dx/len*back+nx*cross,tip.Y-dy/len*back+ny*cross);
                            if(k==0)g.BeginFigure(pt,false,false);else g.LineTo(pt,true,false);
                        }
                    }
                    var brush=new LinearGradientBrush();brush.MappingMode=BrushMappingMode.Absolute;
                    brush.StartPoint=new Point(tip.X-dx/len*length,tip.Y-dy/len*length);brush.EndPoint=tip;
                    brush.GradientStops.Add(new GradientStop(Color.FromArgb(0,195,214,229),0));
                    brush.GradientStops.Add(new GradientStop(Color.FromArgb((byte)(core?166:61),205,223,234),.28));
                    brush.GradientStops.Add(new GradientStop(Color.FromArgb((byte)(core?250:166),248,252,255),1));
                    dc.DrawGeometry(null,new Pen(brush,core?.85-j*.15:.22+(j%4)*.075),geometry);
                }
                return;
            }
        }
        int index=Math.Min(359,(int)(t*60));if(index==last)return;
        last=index;image.Source=bitmaps[index];image.Width=frames[index].width/2.0;image.Height=frames[index].height/2.0;Canvas.SetLeft(image,frames[index].x/2.0);Canvas.SetTop(image,frames[index].y/2.0);
        if(index==359){
            CompositionTarget.Rendering-=Render;
            double scale=Math.Min(ActualWidth/stage.Width,ActualHeight/stage.Height);
            double frameBottom=(frames[index].y+frames[index].height)/2.0;
            double letterboxTop=(ActualHeight-stage.Height*scale)/2;
            workspace.Margin=new Thickness(0,letterboxTop+frameBottom*scale+12,0,0);
            workspace.Visibility=Visibility.Visible;
        }
    }
    [ComImport, Guid("2e941141-7f97-4756-ba1d-9decde894a3d"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IApplicationActivationManager
    {
        void ActivateApplication([MarshalAs(UnmanagedType.LPWStr)] string appId,
            [MarshalAs(UnmanagedType.LPWStr)] string arguments, uint options, out uint processId);
    }

    static void OpenNativeCodex()
    {
        var activationType=Type.GetTypeFromCLSID(new Guid("45ba127d-10a8-46ea-8ab7-56ea9078943c"));
        var activation=(IApplicationActivationManager)Activator.CreateInstance(activationType);
        try{
            uint processId;
            activation.ActivateApplication("OpenAI.Codex_2p2nqsd0c76g0!App",null,0,out processId);
        }finally{
            Marshal.ReleaseComObject(activation);
        }
    }

    void OpenWorkspace()
    {
        try{
            OpenNativeCodex();
            Close();
        }catch(Exception ex){
            MessageBox.Show("无法打开 Codex。请确认已安装 Windows 版 Codex。"+Environment.NewLine+ex.Message,"Spider Codex");
        }
    }
}
