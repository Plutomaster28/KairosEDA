// Simple counter with enable and reset
// Example RTL for Kairos EDA demonstration

module counter_8bit (
    input  wire       clk,
    input  wire       rst_n,
    input  wire       enable,
    output reg  [7:0] count,
    output wire       overflow
);

    always @(posedge clk or negedge rst_n) begin
        if (!rst_n) begin
            count <= 8'd0;
        end else if (enable) begin
            count <= count + 8'd1;
        end
    end
    
    assign overflow = (count == 8'd255) && enable;

endmodule
