ALTER TABLE company_subscriptions ADD COLUMN pending_plan varchar(20) NULL;
ALTER TABLE company_subscriptions ADD COLUMN pending_plan_amount_usd numeric(10,2) NULL;
ALTER TABLE company_subscriptions ADD COLUMN pending_usd_ars_rate numeric(10,2) NULL;
ALTER TABLE company_subscriptions ADD COLUMN pending_amount_ars numeric(10,2) NULL;
ALTER TABLE company_subscriptions ADD COLUMN pending_preapproval_id varchar(100) NULL;
