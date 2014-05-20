using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Description;
using NHibernate;
using Contracts;
using IAccountRepositoryService.Domain;
using log4net;
using System.Timers;

namespace IAccountRepositoryService
{
    class Program
    {
        private const string serviceAddress = "net.tcp://localhost:54390/IAccountRepository";
        private const string serviceRepositoryAddress = "net.tcp://localhost:54390/IServiceRepository";
        private const string serviceName = "IAccountRepository";
        private const string ipAddress = "localhost"; //IP komputera w sali, localhost do testów
        private static IServiceRepository repository { get; set; }

        static void Main(string[] args)
        {
            //Logger
            log4net.Config.XmlConfigurator.Configure();

            //Configuration
            AccountRepository accountRep = new AccountRepository();
            ServiceHost sh = new ServiceHost(accountRep, new Uri[] { new Uri(serviceAddress) });
            sh.AddServiceEndpoint(typeof(IAccountRepository), new NetTcpBinding(SecurityMode.None), serviceAddress.Replace("localhost", "0.0.0.0"));
            ServiceMetadataBehavior metadata = sh.Description.Behaviors.Find<ServiceMetadataBehavior>();
            if (metadata == null)
            {
                metadata = new ServiceMetadataBehavior();
                sh.Description.Behaviors.Add(metadata);
            }
            metadata.MetadataExporter.PolicyVersion = PolicyVersion.Policy15;
            sh.AddServiceEndpoint(ServiceMetadataBehavior.MexContractName, MetadataExchangeBindings.CreateMexTcpBinding(), "mex");


            //Service starting
            sh.Open();
            Logger.log.Info("IAccountRepository started!");


            //Register service in IServiceRepository
            ChannelFactory<IServiceRepository> cf = new ChannelFactory<IServiceRepository>(new NetTcpBinding(SecurityMode.None), serviceRepositoryAddress);
            repository = cf.CreateChannel();
            Logger.log.Info("Connection with IServiceRepository completed!");

            repository.registerService(serviceName, serviceAddress.Replace("localhost", ipAddress));
            Logger.log.Info("Service registered!");


            //Send info for IServiceRepository
            AliveSignal();
            Logger.log.Info("Alive");


            //Sending information that service is alive every 10 minutes
            Timer t = new Timer(1000 * 60 * 10); // 1000 milisecond * 60 = 1 minute
            t.AutoReset = true;
            t.Elapsed += new System.Timers.ElapsedEventHandler(Alive);
            t.Start();


            //Click to close service
            Console.ReadLine();


            //Unregisted service from IServiceRepository
            repository.unregisterService(serviceName);

            //Service closing
            Logger.log.Info("IAccountRepository closing!");
        }

        private static void Alive(object sender, System.Timers.ElapsedEventArgs e)
        {
            AliveSignal();       
        }

        private static void AliveSignal()
        {
            try
            {
                repository.isAlive(serviceName);
                Logger.log.Info("Alive Success");
            }
            catch (Exception ex)
            {
                Logger.log.Error("IsAlive Error - Message: " + ex.Message);
            }
        }
    }

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, IncludeExceptionDetailInFaults = true)]
    public class AccountRepository : IAccountRepository
    {
        public string CreateAccount(Guid clientId, AccountDetails details)
        {
            try
            {
                using (ISession session = NHibernateHelper.OpenSession())
                {
                    using (ITransaction transaction = session.BeginTransaction())
                    {
                        Account account = new Account(details);
                        account.ClientId = clientId;

                        string number = "";

                        bool flag = false;
                        while (!flag)
                        {
                            Random rnd = new Random();
                            int nr = rnd.Next(100000000, 999999999);
                            number = String.Format("{0}{0}{1}", nr.ToString(), nr.ToString().Substring(0, nr.ToString().Length - 1));
                            AccountDetails dt = GetAccountInformation(number);
                            if (dt == null) flag = true;
                        }

                        account.AccountNumber = number;

                        session.Save(account);
                        transaction.Commit();

                        Logger.log.Info("CreateAccount Completed - Account: " + number);
                        return account.AccountNumber;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.log.Error("CreateAccount Error - Message: " + ex.Message);
                return null;
            }
        }

        public AccountDetails GetAccountInformation(string accountNumber)
        {
            try
            {
                using (ISession session = NHibernateHelper.OpenSession())
                {
                    Account result = session.QueryOver<Account>().Where(x => x.AccountNumber == accountNumber).SingleOrDefault();

                    if (result != null)
                    {
                        AccountDetails dt = new AccountDetails();
                        dt.AccountNumber = result.AccountNumber;
                        dt.ClientId = result.ClientId;
                        dt.EndDate = result.EndDate;
                        dt.Id = result.Id;
                        dt.Money = result.Money;
                        dt.Percentage = result.Percentage;
                        dt.StartDate = result.StartDate;
                        dt.Type = result.Type;

                        Logger.log.Info("GetAccountInformation Completed -  Account: " + accountNumber);
                        return dt;
                    }
                    else
                    {
                        Logger.log.Info("GetAccountInformation Failed - Account: " + accountNumber);
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.log.Error("GetAccountInformation Error - Account: " + accountNumber + ", Message: " + ex.Message);
                return null;
            }
        }

        public void UpdateAccountInformation(AccountDetails details)
        {
            try
            {
                using (ISession session = NHibernateHelper.OpenSession())
                {
                    using (ITransaction transaction = session.BeginTransaction())
                    {
                        Account ac = new Account(details);

                        session.Update(ac);
                        transaction.Commit();

                        Logger.log.Info("UpdateAccountInformation Completed -  Account: " + details.AccountNumber);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.log.Error("UpdateAccountInformation Error - Account: " + details.AccountNumber + ", Message: " + ex.Message);
            }
        }
    }

    public class Logger
    {
        internal static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    }
}
