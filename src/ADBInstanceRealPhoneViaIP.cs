namespace NonogramAutomation
{
    public class ADBInstanceRealPhoneViaIP : ADBInstanceViaIP
    {
        public override string LogHeader
        {
            get => $"{Name} | {IP}:{Port}";
        }
    }
}
