// Code generated by jtd-codegen for Rust v0.2.1

use serde::{Deserialize, Serialize};

#[derive(Serialize, Deserialize)]
#[serde(tag = "incomeType")]
pub enum Income {
    #[serde(rename = "EmploymentIncome")]
    EmploymentIncome(IncomeEmploymentIncome),

    #[serde(rename = "PassiveIncome")]
    PassiveIncome(IncomePassiveIncome),
}
