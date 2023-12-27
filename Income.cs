namespace TypeGen;

public abstract class Income
{
	public decimal Amount { get; set; }
	public string Source { get; set; }
	public abstract IncomeType IncomeType { get; }
}

public enum IncomeType
{
	Employment,
	Passive
}

public class EmploymentIncome : Income
{
	public override IncomeType IncomeType => IncomeType.Employment;
	public EmploymentType EmploymentType { get; set; }
}

public enum EmploymentType
{
	Salaried,
	Hourly,
	Contract
}

public class PassiveIncome : Income
{
	public PassiveIncomeType PassiveIncomeType { get; set; }
	public override IncomeType IncomeType => IncomeType.Passive;
}

public enum PassiveIncomeType
{
	Investment,
	Rental,
	Royalties
}
