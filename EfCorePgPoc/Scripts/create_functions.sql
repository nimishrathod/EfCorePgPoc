-- Function 1: Simple Scalar Function
CREATE OR REPLACE FUNCTION ticketing.tickets_left(p_ticket_type_id uuid)
RETURNS NUMERIC
LANGUAGE sql
as $$
    SELECT tt.available_quantity
    FROM ticketing.ticket_types tt
    WHERE tt."Id" = p_ticket_type_id
$$;

-- Function 2: Table-valued Function
CREATE OR REPLACE FUNCTION ticketing.customer_order_summary(p_customer_id uuid)
returns TABLE (
    order_id uuid,
    created_at_utc timestamptz,
    total_price numeric,
    currency text,
    item_count numeric
)
LANGUAGE sql
AS $$
    SELECT o."Id", o.created_at_utc, o.total_price,
        o.currency, COALESCE(sum(oi.quantity), 0) as item_count
    from ticketing.orders o
    left join ticketing.order_items oi on oi.order_id = o."Id"
    where o.customer_id = p_customer_id
    group by o."Id", o.created_at_utc, o.total_price, o.currency
    order by o.created_at_utc DESC
$$;

-- Procedure: Adjust Quantity with Validation
CREATE OR REPLACE PROCEDURE ticketing.adjust_available_quantity(
    p_ticket_type_id uuid,
    p_delta numeric,
    p_reason text DEFAULT 'manual-adjust'
)
LANGUAGE plpgsql
AS $$
DECLARE
    v_qty numeric;
    v_avail numeric;
    v_new_avail numeric;
BEGIN
    select quantity, available_quantity
    into v_qty, v_avail
    from ticketing.ticket_types
    where id = p_ticket_type_id
    for update;

    if NOT FOUND THEN
        raise exception 'ticket_type % not found', p_ticket_type_id;
    end if;

    v_new_avail := v_avail + p_delta;

    if v_new_avail < 0 THEN
        raise exception 'Cannot reduce below zero';
    end if;

    if v_new_avail > v_qty THEN
        raise exception 'Cannot exceed quantity';
    end if;

    update ticketing.ticket_types
    set available_quantity = v_new_avail
    where Id = p_ticket_type_id;
END;
$$;