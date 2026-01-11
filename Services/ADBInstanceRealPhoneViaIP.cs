namespace NonogramAutomation.Services
{
    public class ADBInstanceRealPhoneViaIP : ADBInstanceViaIP
    {
        protected override string LogHeader
        {
            get => $"{Name} | {IP}:{Port}";
        }
    }
}
