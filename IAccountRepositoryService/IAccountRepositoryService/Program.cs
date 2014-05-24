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
    class Program//get all accounts
    {
        private const string serviceAddress = "net.tcp://localhost:50000/IAccountRepository";
        private const string serviceRepositoryAddress = "net.tcp://192.168.0.109:50000/IServiceRepository";
        private const string serviceName = "IAccountRepository";
        private const string ipAddress = "192.168.0.114"; //IP komputera w sali, localhost do testów
        private const bool test = true;
        private static IServiceRepository repository { get; set; }
        private static IClientRepository clientService { get; set; }

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
            if (!test)
            {
                Reactivate();


                //Sending information that service is alive every 10 minutes
                Timer t = new Timer(1000 * 5); // 1000 milisecond * 60 = 1 minute
                t.AutoReset = true;
                t.Elapsed += new System.Timers.ElapsedEventHandler(Alive);
                t.Start();
            }

            //Click to close service
            Console.ReadLine();


            //Unregisted service from IServiceRepository
            if (!test)
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
                //Logger.log.Info("Alive Success");
            }
            catch (Exception ex)
            {
                Logger.log.Error("IsAlive Error - Message: " + ex.Message);
                Reactivate();
            }
        }

        private static void Reactivate()
        {
            ChannelFactory<IServiceRepository> cf = new ChannelFactory<IServiceRepository>(new NetTcpBinding(SecurityMode.None), serviceRepositoryAddress);
            repository = cf.CreateChannel();
            Logger.log.Info("Connection with IServiceRepository completed!");
            Console.WriteLine("Connected with IServiceRepository");

            repository.registerService(serviceName, serviceAddress.Replace("localhost", ipAddress));
            Logger.log.Info("Service registered!");


            //Send info for IServiceRepository
            AliveSignal();
            Logger.log.Info("Alive");
        }


        [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, IncludeExceptionDetailInFaults = true)]
        public class AccountRepository : IAccountRepository
        {
            public string CreateAccount(Guid clientId, AccountDetails details)
            {
                string clientRepositoryAddress = String.Empty;
                try
                {
                    if (!test)
                        clientRepositoryAddress = repository.getServiceAddress("IClientRepository");
                    if (test || !String.IsNullOrEmpty(clientRepositoryAddress))
                    {
                        ServiceClient info;
                        if (!test)
                        {
                            ChannelFactory<IClientRepository> cf2 = new ChannelFactory<IClientRepository>(new NetTcpBinding(SecurityMode.None), clientRepositoryAddress);
                            clientService = cf2.CreateChannel();

                            info = clientService.GetClientInformationById(clientId);
                        }

                        if (test || info.IdClient != Guid.Empty)
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
                    }
                    Logger.log.Error("CreateAccount Failed");
                    return null;
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

            public bool UpdateAccountInformation(AccountDetails details)
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
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.log.Error("UpdateAccountInformation Error - Account: " + details.AccountNumber + ", Message: " + ex.Message);
                    return false;
                }
            }

            public List<AccountDetails> GetAccountsById(Guid clientId)
            {
                try
                {
                    using (ISession session = NHibernateHelper.OpenSession())
                    {
                        List<Account> result = session.QueryOver<Account>().Where(x => x.ClientId == clientId).List().ToList();

                        List<AccountDetails> list = new List<AccountDetails>();
                        if (result != null)
                        {
                            foreach (Account a in result)
                            {
                                AccountDetails dt = new AccountDetails();
                                dt.AccountNumber = a.AccountNumber;
                                dt.ClientId = a.ClientId;
                                dt.EndDate = a.EndDate;
                                dt.Id = a.Id;
                                dt.Money = a.Money;
                                dt.Percentage = a.Percentage;
                                dt.StartDate = a.StartDate;
                                dt.Type = a.Type;
                                list.Add(dt);
                            }

                            Logger.log.Info("GetAccountsById Completed -  ClientId: " + clientId);
                            return list;
                        }
                        else
                        {
                            Logger.log.Info("GetAccountsById Failed - ClientId: " + clientId);
                            return null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.log.Error("GetAccountsById Error - ClientId: " + clientId + ", Message: " + ex.Message);
                    return null;
                }
            }

            public List<AccountDetails> GetAllAccounts()
            {
                try
                {
                    using (ISession session = NHibernateHelper.OpenSession())
                    {
                        List<Account> result = session.QueryOver<Account>().List().ToList();
                        if (result != null)
                        {
                            List<AccountDetails> list = new List<AccountDetails>();
                            foreach (Account a in result)
                            {
                                AccountDetails dt = new AccountDetails();
                                dt.AccountNumber = a.AccountNumber;
                                dt.ClientId = a.ClientId;
                                dt.EndDate = a.EndDate;
                                dt.Id = a.Id;
                                dt.Money = a.Money;
                                dt.Percentage = a.Percentage;
                                dt.StartDate = a.StartDate;
                                dt.Type = a.Type;
                                list.Add(dt);
                            }
                            Logger.log.Info("GetAllAccounts Completed");
                            return list;
                        }
                        else
                        {
                            Logger.log.Info("GetAllAccounts Failed");
                            return null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.log.Error("GetAllAccounts Error - Message: " + ex.Message);
                    return null;
                }
            }

        }
    }



    public class Logger
    {
        internal static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    }
}
