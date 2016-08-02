using BORep.BusinessSystemCenter.ServiceRouting.ServiceInformations;
using Microsoft.Web.Administration;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace CreateIISSite
{
    public class IIS7Helper
    {
        #region Property
        protected ServerManager sm;
        private string _SiteName;
        public string SiteName
        {
            get
            {
                return _SiteName;
            }
            set
            {
                this._SiteName = value;

            }
        }
        public int Port;
        public string PhysicsPath;
        protected System.Configuration.Configuration webConfig;
        protected string[] invalidPath = new string[] { "~packages", "aspnet_client", "Tools" };
        ServiceInformations _ServiceInformations;
        /// <summary>
        /// 服务信息
        /// </summary>
        public ServiceInformations ServiceInformations
        {
            get
            {
                if (_ServiceInformations == null)
                {
                    _ServiceInformations = new ServiceInformations();
                }
                return _ServiceInformations;
            }
        }

        const string CONFIG_FILE_NAME = "ServiceInformations.config";
        const string MY_CONFIG_FILE_NAME = "my.ServiceInformations.config";
        #endregion

        #region New
        public IIS7Helper(int port, string physicsPath, string siteName)
        {
            this.SiteName = siteName;
            this.Port = port;
            this.PhysicsPath = physicsPath;
            sm = new ServerManager();
        }
        public IIS7Helper(int port, string physicsPath)
        {
            this.Port = port;
            this.PhysicsPath = physicsPath;
            this.SiteName = System.IO.Path.GetFileName(physicsPath);
            sm = new ServerManager();
        }
        #endregion

        #region Public
        public void CreateSite()
        {
            try
            {
                if (Check())
                {
                    ApplicationPool appPool = CreateApplicationPool(SiteName);
                    Site site = CreateSite(SiteName, Port, PhysicsPath);

                    string[] directories = Directory.GetDirectories(PhysicsPath);
                    var items = directories.ToList().Where(c => !invalidPath.ToList().Contains(System.IO.Path.GetFileName(c)));
                    foreach (var item in items)
                    {
                        string appName = System.IO.Path.GetFileName(item);
                        CreateApplication(appName, site, "/" + appName, item, appPool.Name);
                    }
                    sm.CommitChanges();
                    var path = string.Format("{0}\\web.config", PhysicsPath);//配置文件路径  
                    webConfig = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration("/", site.Name);
                    SetAppSetting("InitialCatalog", site.Name);
                    SetAppSetting("ServicesRootFolder", PhysicsPath);
                    webConfig.Save();
                }
            }
            catch (Exception error)
            {
                throw;
            }
        }

        public void EditServiceInformation(string oldPort, int Port)
        {
            string ServiceInformationPath = Path.Combine(PhysicsPath, "SystemCenter", CONFIG_FILE_NAME);
            if (System.IO.File.Exists(ServiceInformationPath))
            {
                LoadConfig(ServiceInformationPath);
                EditConfig(oldPort,Port);
                SaveConfig(ServiceInformationPath);
            }
            ServiceInformationPath = Path.Combine(PhysicsPath, "SystemCenter", MY_CONFIG_FILE_NAME);
            if (System.IO.File.Exists(ServiceInformationPath))
            {
                LoadConfig(ServiceInformationPath);
                EditConfig(oldPort,Port);
                SaveConfig(ServiceInformationPath);
            }
        }
        #endregion

        #region Operation
        protected virtual ApplicationPool CreateApplicationPool(string appPoolName, string runtimeVersion = "v4.0", ManagedPipelineMode mode = ManagedPipelineMode.Integrated)
        {
            try
            {
                ApplicationPool newPool = sm.ApplicationPools[appPoolName];
                Microsoft.Web.Administration.Configuration config = sm.GetApplicationHostConfiguration();
                if (newPool != null)
                {
                    sm.ApplicationPools.Remove(newPool);
                }
                newPool = sm.ApplicationPools.Add(appPoolName);
                newPool.ManagedRuntimeVersion = runtimeVersion;
                newPool.ManagedPipelineMode = mode;
                newPool.Enable32BitAppOnWin64 = true;
                return newPool;
            }
            catch (Exception error)
            {
                throw new Exception(string.Format("创建应用程序池时出错:{0}", error.Message));
            }
        }
        protected virtual Site CreateSite(string siteName, int port, string physicsPath, string protocolName = "http")
        {
            try
            {
                Site site = sm.Sites[siteName];
                if (site != null)
                {
                    sm.Sites.Remove(site);
                }
                site = sm.Sites.Add(siteName, protocolName, string.Format("*:{0}:", port), physicsPath);
                site.Applications[0].ApplicationPoolName = siteName;
                return site;
            }
            catch (Exception error)
            {
                throw new Exception(string.Format("创建网站时出错:{0}", error.Message));
            }
        }
        protected virtual Application CreateApplication(string appName, Site site, string Path, string physicsPath, string appPoolName)
        {
            try
            {
                Microsoft.Web.Administration.Configuration config = sm.GetApplicationHostConfiguration();
                Application newApp = site.Applications[Path];
                if (newApp != null)
                {
                    site.Applications.Remove(newApp);  //delete this application
                    config.RemoveLocationPath(string.Format("{0}{1}", site.Name, Path)); //delete the node of the applicationHostConfig.config file with this application
                }
                newApp = site.Applications.Add(Path, physicsPath);
                newApp.ApplicationPoolName = appPoolName;

                //开启目录浏览
                string path = "system.webServer/directoryBrowse";//the attribue path in the applictionHostConfig.config file.
                Microsoft.Web.Administration.ConfigurationSection dbS = config.GetSection(path, string.Format("{0}{1}", site.Name, Path));
                dbS.Attributes["enabled"].Value = true;
                return newApp;
            }
            catch (Exception error)
            {
                throw new Exception(string.Format("创建应用程序[{0}]时出错:{1}", appName, error.Message));
            }
        }

        /// <summary>
        /// 读取配置信息
        /// </summary>
        void LoadConfig(string ServiceInformationPath)
        {
            if (!System.IO.File.Exists(ServiceInformationPath)) return;
            var Serializer = new System.Runtime.Serialization.DataContractSerializer(this.ServiceInformations.GetType());

            using (var file = System.IO.File.OpenRead(ServiceInformationPath))
            {
                var tmpConfig = Serializer.ReadObject(file) as ServiceInformations;
                file.Close();
                this.ServiceInformations.AddRange(tmpConfig);
            }
        }
        /// <summary>
        /// 读取配置信息
        /// </summary>
        void EditConfig(string oldPort, int Port)
        {
            if (this.ServiceInformations == null) return;
            foreach (ServiceInformation ServiceInformation in this.ServiceInformations)
            {
                foreach (ServiceProvider ServiceProvider in ServiceInformation.RegisteredServiceProviders)
                {
                    ServiceProvider.RootAddress = ServiceProvider.RootAddress.Replace(oldPort, string.Format(":{0}",Port));
                }
            }
        }
        /// <summary>
        /// 保存配置信息
        /// </summary>
        void SaveConfig(string ServiceInformationPath)
        {

            var Serializer = new System.Runtime.Serialization.DataContractSerializer(this.ServiceInformations.GetType());
            var file = System.IO.File.Create(ServiceInformationPath);
            Serializer.WriteObject(file, this.ServiceInformations);
            file.Close();
        }
        #endregion

        #region Check


        /// <summary>
        /// 设置应用程序配置节点,如果已经存在此节点,则会修改该节点的值,否则添加此节点  
        /// </summary>
        /// <param name="key">节点名称</param>
        /// <param name="value">节点值</param>
        public void SetAppSetting(string key, string value)
        {
            AppSettingsSection appSetting = (AppSettingsSection)webConfig.GetSection("appSettings");
            if (appSetting.Settings[key] == null)//如果不存在此节点,则添加  
            {
                appSetting.Settings.Add(key, value);
            }
            else//如果存在此节点,则修改  
            {
                appSetting.Settings[key].Value = value;
            }
        }

        protected virtual bool Check()
        {
            try
            {
                if (!Directory.Exists(this.PhysicsPath)) throw new Exception("配置网站路径不存在");
                if (PortInUse(this.Port))
                    throw new Exception(string.Format("[{0}]端口已被占用,请使用其他端口", this.Port));
                //Site site = sm.Sites[this.SiteName];
                //if (site != null) throw new Exception(string.Format("已存在名字为[{0}]的网站", this.SiteName));
                return true;
            }
            catch (Exception error)
            {
                throw;
            }
        }

        public static bool PortInUse(int port)
        {
            bool inUse = false;

            IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] ipEndPoints = ipProperties.GetActiveTcpListeners();

            foreach (IPEndPoint endPoint in ipEndPoints)
            {
                if (endPoint.Port == port)
                {
                    inUse = true;
                    break;
                }
            }
            return inUse;
        }

        #endregion

    }

}
