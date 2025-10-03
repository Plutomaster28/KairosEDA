// Simple state machine - Traffic light controller
// Example RTL for Kairos EDA demonstration

module traffic_light (
    input  wire clk,
    input  wire rst_n,
    input  wire sensor,        // Car detected on side street
    output reg  [2:0] main_light,   // [2]=red, [1]=yellow, [0]=green
    output reg  [2:0] side_light
);

    // State encoding
    localparam MAIN_GREEN  = 3'b000;
    localparam MAIN_YELLOW = 3'b001;
    localparam SIDE_GREEN  = 3'b010;
    localparam SIDE_YELLOW = 3'b011;
    
    reg [1:0] state;
    reg [7:0] timer;
    
    // Light patterns
    localparam RED    = 3'b100;
    localparam YELLOW = 3'b010;
    localparam GREEN  = 3'b001;
    
    always @(posedge clk or negedge rst_n) begin
        if (!rst_n) begin
            state <= MAIN_GREEN;
            timer <= 8'd0;
            main_light <= GREEN;
            side_light <= RED;
        end else begin
            timer <= timer + 1;
            
            case (state)
                MAIN_GREEN: begin
                    main_light <= GREEN;
                    side_light <= RED;
                    if (sensor && timer > 100) begin
                        state <= MAIN_YELLOW;
                        timer <= 8'd0;
                    end
                end
                
                MAIN_YELLOW: begin
                    main_light <= YELLOW;
                    side_light <= RED;
                    if (timer > 20) begin
                        state <= SIDE_GREEN;
                        timer <= 8'd0;
                    end
                end
                
                SIDE_GREEN: begin
                    main_light <= RED;
                    side_light <= GREEN;
                    if (timer > 50) begin
                        state <= SIDE_YELLOW;
                        timer <= 8'd0;
                    end
                end
                
                SIDE_YELLOW: begin
                    main_light <= RED;
                    side_light <= YELLOW;
                    if (timer > 20) begin
                        state <= MAIN_GREEN;
                        timer <= 8'd0;
                    end
                end
            endcase
        end
    end

endmodule
