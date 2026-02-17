shader_type spatial;
render_mode unshaded, cull_disabled;

uniform vec4 grid_color : source_color = vec4(0.15, 0.25, 0.4, 0.3);
uniform vec4 major_grid_color : source_color = vec4(0.2, 0.35, 0.5, 0.5);
uniform float grid_spacing = 100.0;
uniform float major_grid_every = 5.0;
uniform float line_width = 1.5;
uniform float fade_distance = 3000.0;

void fragment() {
    vec3 world_pos = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;

    // Minor grid
    vec2 grid_uv = abs(fract(world_pos.xz / grid_spacing - 0.5) - 0.5);
    vec2 line = grid_uv * grid_spacing;
    float minor = min(line.x, line.y);
    float minor_alpha = 1.0 - smoothstep(0.0, line_width, minor);

    // Major grid
    vec2 major_uv = abs(fract(world_pos.xz / (grid_spacing * major_grid_every) - 0.5) - 0.5);
    vec2 major_line = major_uv * grid_spacing * major_grid_every;
    float major = min(major_line.x, major_line.y);
    float major_alpha = 1.0 - smoothstep(0.0, line_width * 2.0, major);

    // Distance fade
    float dist = length(world_pos.xz - (INV_VIEW_MATRIX * vec4(0, 0, 0, 1)).xz);
    float fade = 1.0 - smoothstep(fade_distance * 0.5, fade_distance, dist);

    // Combine
    vec4 color = mix(grid_color * minor_alpha, major_grid_color, major_alpha);
    ALBEDO = color.rgb;
    ALPHA = max(minor_alpha * grid_color.a, major_alpha * major_grid_color.a) * fade;
}
