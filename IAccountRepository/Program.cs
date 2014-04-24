using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Runtime.Serialization;

namespace IAccountRepository
{
    class Program
    {
        static void Main(string[] args)
        {
            AccountRepository accountRep = new AccountRepository();
            ServiceHost sh = new ServiceHost(accountRep, new Uri[] { new Uri("net.tcp://localhost:54398/IAccountRepository") });
            sh.AddServiceEndpoint(typeof(IAccountRepository), new NetTcpBinding(SecurityMode.None), "net.tcp://0.0.0.0:54398/IAccountRepository");
            ServiceMetadataBehavior metadata = sh.Description.Behaviors.Find<ServiceMetadataBehavior>();
            if (metadata == null)
            {
                metadata = new ServiceMetadataBehavior();
                sh.Description.Behaviors.Add(metadata);
            }
            metadata.MetadataExporter.PolicyVersion = PolicyVersion.Policy15;
            sh.AddServiceEndpoint(ServiceMetadataBehavior.MexContractName, MetadataExchangeBindings.CreateMexTcpBinding(), "mex");
            sh.Open();

            Console.ReadLine();
        }
    }

    [ServiceContract]
    public interface IAccountRepository
    {
        [OperationContract]
        long createAccount(int clientId, AccountDetails details);

        [OperationContract]
        AccountDetails getAccountInformation(long accountNumber);
    }

    [DataContract(Namespace = "IAccountRepository")]
    public class AccountDetails
    {
        [DataMember]
        public int ClientId { get; set; }
        [DataMember]
        public long AccountNumber { get; set; }
        [DataMember]
        public string FirstName { get; set; }
        [DataMember]
        public string LastName { get; set; }
        [DataMember]
        public long Money { get; set; }
        [DataMember]
        public string Pesel { get; set; }
        [DataMember]
        public string Address { get; set; }
        [DataMember]
        public string PhoneNumber { get; set; }
        [DataMember]
        public string Type { get; set; }
        [DataMember]
        public double Percentage { get; set; }
        [DataMember]
        public DateTime EndDate { get; set; }
        [DataMember]
        public DateTime StartDate { get; set; }

    }

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, IncludeExceptionDetailInFaults = true)]
    public class AccountRepository : IAccountRepository
    {
        public long createAccount(int clientId, AccountDetails details)
        {
            //zalozenie konta w bazie i zwrocenie id konta
            // TO DO

            return 1234567890;
        }

        public AccountDetails getAccountInformation(long accountNumber)
        {
            //zwrocenie informacji o koncie z bazy
            // TO DO

            AccountDetails ad = new AccountDetails();
            ad.Address = "Krakowska 1, Kraków";
            ad.ClientId = 123;
            ad.EndDate = new DateTime(2015, 12, 1);
            ad.StartDate = DateTime.Now;
            ad.FirstName = "Jan";
            ad.LastName = "Kowalski";
            ad.Money = 100;
            ad.Percentage = 3;
            ad.Pesel = "92010112345";
            ad.PhoneNumber = "100 200 300";
            ad.Type = "ROR";


            return ad;
        }
    }
}
