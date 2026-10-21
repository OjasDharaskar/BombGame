import os
from pptx import Presentation
from pptx.util import Inches, Pt
from pptx.dml.color import RGBColor
from pptx.enum.text import PP_ALIGN
from pptx.enum.shapes import MSO_SHAPE

prs = Presentation()
prs.slide_width = Inches(13.333)
prs.slide_height = Inches(7.5)
blank_layout = prs.slide_layouts[6]

img_dir = r"C:\Users\ojasd\.gemini\antigravity\brain\9f326fb1-1dca-49cf-994e-991c8eafed4b\images"

BG_COLOR = RGBColor(22, 18, 26)
CARD_BG = RGBColor(36, 30, 42)
CARD_BORDER = RGBColor(65, 52, 75)
GOLD = RGBColor(255, 180, 50)
ORANGE = RGBColor(255, 110, 50)
RED = RGBColor(235, 75, 75)
WHITE = RGBColor(245, 245, 250)
MUTED = RGBColor(180, 175, 190)
GREEN = RGBColor(70, 210, 120)

def set_slide_background(slide):
    bg = slide.shapes.add_shape(MSO_SHAPE.RECTANGLE, Inches(0), Inches(0), Inches(13.333), Inches(7.5))
    bg.fill.solid()
    bg.fill.fore_color.rgb = BG_COLOR
    bg.line.fill.background()
    return bg

def add_header(slide, title_text, category_text="THE BOMB GAME"):
    cat_box = slide.shapes.add_textbox(Inches(0.8), Inches(0.4), Inches(11.7), Inches(0.35))
    tf_cat = cat_box.text_frame
    tf_cat.word_wrap = True
    p_cat = tf_cat.paragraphs[0]
    p_cat.text = category_text.upper()
    p_cat.font.size = Pt(11)
    p_cat.font.bold = True
    p_cat.font.color.rgb = ORANGE

    title_box = slide.shapes.add_textbox(Inches(0.8), Inches(0.7), Inches(11.7), Inches(0.75))
    tf = title_box.text_frame
    tf.word_wrap = True
    p = tf.paragraphs[0]
    p.text = title_text
    p.font.size = Pt(26)
    p.font.bold = True
    p.font.color.rgb = GOLD

def add_card(slide, left, top, width, height, title, items, image_path=None, border_color=None):
    card = slide.shapes.add_shape(MSO_SHAPE.ROUNDED_RECTANGLE, left, top, width, height)
    card.fill.solid()
    card.fill.fore_color.rgb = CARD_BG
    card.line.color.rgb = border_color if border_color else CARD_BORDER
    card.line.width = Pt(1.5)

    offset_top = top + Inches(0.18)
    text_width = width - Inches(0.4)

    if image_path and os.path.exists(image_path):
        img_w = Inches(0.85)
        slide.shapes.add_picture(image_path, left + Inches(0.2), offset_top, width=img_w)
        tb = slide.shapes.add_textbox(left + Inches(1.15), offset_top - Inches(0.05), width - Inches(1.3), Inches(0.5))
    else:
        tb = slide.shapes.add_textbox(left + Inches(0.25), offset_top, text_width, Inches(0.5))

    p = tb.text_frame.paragraphs[0]
    p.text = title
    p.font.size = Pt(17)
    p.font.bold = True
    p.font.color.rgb = GOLD

    body_top = offset_top + Inches(0.6) if image_path else offset_top + Inches(0.5)
    tb_body = slide.shapes.add_textbox(left + Inches(0.25), body_top, width - Inches(0.5), height - (body_top - top) - Inches(0.15))
    tf_body = tb_body.text_frame
    tf_body.word_wrap = True

    for i, item in enumerate(items):
        p_item = tf_body.paragraphs[0] if i == 0 else tf_body.add_paragraph()
        p_item.text = f"-  {item}"
        p_item.font.size = Pt(12)
        p_item.font.color.rgb = WHITE
        p_item.space_after = Pt(6)

# ----------------- SLIDE 1: Title & Overview -----------------
s1 = prs.slides.add_slide(blank_layout)
set_slide_background(s1)

tb = s1.shapes.add_textbox(Inches(0.8), Inches(1.6), Inches(7.5), Inches(2.2))
tf = tb.text_frame
tf.word_wrap = True
p = tf.paragraphs[0]
p.text = "THE BOMB GAME"
p.font.size = Pt(44)
p.font.bold = True
p.font.color.rgb = GOLD

p2 = tf.add_paragraph()
p2.text = "Pirate Action-Platformer in Unity 6 & URP 2D"
p2.font.size = Pt(20)
p2.font.color.rgb = ORANGE
p2.space_before = Pt(10)

p3 = tf.add_paragraph()
p3.text = "Technical Post-Mortem, Systems Architecture, and Design Analysis"
p3.font.size = Pt(13)
p3.font.color.rgb = MUTED
p3.space_before = Pt(6)

gp_img = os.path.join(img_dir, "gameplay.png")
if os.path.exists(gp_img):
    s1.shapes.add_picture(gp_img, Inches(8.0), Inches(1.2), width=Inches(4.6))

add_card(s1, Inches(0.8), Inches(4.5), Inches(3.7), Inches(2.4), "Core Objective", 
         ["Navigate multi-level pirate fortresses", "Use timed bombs to defeat unique pirates", "Reach exit doors to progress across stages"])
add_card(s1, Inches(4.7), Inches(4.5), Inches(3.7), Inches(2.4), "Engine & Tech", 
         ["Engine: Unity 6 (6000.4.1f1)", "Graphics: Universal Render Pipeline (URP 2D)", "C# Custom Event & Physics Framework"])
add_card(s1, Inches(8.6), Inches(4.5), Inches(4.0), Inches(2.4), "Sensory Style", 
         ["Authentic 16-bit retro pirate pixel art", "Dynamic 2D volumetric light sources", "Responsive sound design with footstep cadence"])

# ----------------- SLIDE 2: Gameplay Goals -----------------
s2 = prs.slides.add_slide(blank_layout)
set_slide_background(s2)
add_header(s2, "Gameplay Goals & Progression", "01. Game Design")

add_card(s2, Inches(0.8), Inches(1.8), Inches(3.7), Inches(5.1), "Primary Goal: Stage Clear",
         ["Navigate from start position to the level exit door.", 
          "Level 1: Climb interior platforms to reach Door at (6.45, 3.87).",
          "Level 2: Conquer multi-tiered gauntlet with high-density enemy placement.",
          "Door trigger initiates scene loading to the next chapter."],
         os.path.join(img_dir, "door.png"))

add_card(s2, Inches(4.8), Inches(1.8), Inches(3.7), Inches(5.1), "Bomb Mastery & Tactical Combat",
         ["Time 3.5s bomb fuses to hit enemies before they react.",
          "Manage projectile arcs and throw force.",
          "Prevent enemy counter-attacks (kicks, catches, swallows).",
          "Blast radius (1.6u) applies both direct damage and physical knockback."],
         os.path.join(img_dir, "bomb.png"))

add_card(s2, Inches(8.8), Inches(1.8), Inches(3.7), Inches(5.1), "Survival & Health Economy",
         ["Start each run with 3 maximum health hearts.",
          "Enemy contact deals 1 heart damage + knockback.",
          "Temporary invulnerability flashing (1.2s) prevents instant death traps.",
          "Bottomless pit boundaries trigger instant restart."],
         os.path.join(img_dir, "health_bar.png"))

# ----------------- SLIDE 3: Key Features -----------------
s3 = prs.slides.add_slide(blank_layout)
set_slide_background(s3)
add_header(s3, "Key Game Features & Mechanics", "02. Core Systems")

add_card(s3, Inches(0.8), Inches(1.8), Inches(5.6), Inches(2.4), "Physics-Driven Bomb Warfare",
         ["Parabolic projectile launch with custom horizontal/vertical impulses.",
          "Dynamic countdown visualizer (charging, pulsing, closing, exploding).",
          "Physics2D OverlapCircle damage and knockback calculation."],
         os.path.join(img_dir, "explosion.png"))

add_card(s3, Inches(6.8), Inches(1.8), Inches(5.7), Inches(2.4), "Reactive Pirate AI Archetypes",
         ["5 distinct enemy behaviors that directly interact with player bombs.",
          "Bald Pirate kicks bombs away; Big Guy catches and throws back.",
          "Whale swallows bombs whole; Captain flees in panic; Cucumber bites."],
         os.path.join(img_dir, "bald_pirate.png"))

add_card(s3, Inches(0.8), Inches(4.5), Inches(5.6), Inches(2.4), "Zero-Downtime Instant Respawn",
         ["Instantaneous restart of the exact current level upon player death.",
          "No disruptive game over popups or blocking overlay screens.",
          "Dedicated 'R' hotkey allows instant manual level restart anytime."],
         os.path.join(img_dir, "player.png"))

add_card(s3, Inches(6.8), Inches(4.5), Inches(5.7), Inches(2.4), "Dedicated Main Menu & Level Flow",
         ["Standalone retro main menu scene with keyboard-first controls.",
          "Interactive selection: Play Level 1, Jump to Level 2, or Quit.",
          "Seamless LevelManager singleton coordinating scenes in background."],
         os.path.join(img_dir, "skull.png"))

# ----------------- SLIDE 4: Controls & Player Experience -----------------
s4 = prs.slides.add_slide(blank_layout)
set_slide_background(s4)
add_header(s4, "Controls and Player Experience", "03. Player Feel")

add_card(s4, Inches(0.8), Inches(1.8), Inches(5.6), Inches(5.1), "Input Mapping (Arcade Feel)",
         ["A / D or Left / Right: Smooth horizontal run (6.5 speed).",
          "Spacebar: Precision jump (11.5 impulse force).",
          "F / J / E / Left Click: Throw ignited bomb with projectile arc.",
          "R Key: Instant active level restart (during life or death).",
          "Menu Navigation: UP / DOWN arrows and ENTER (per specification).",
          "1 / 2 Keys: Quick level switching for development and test."],
         os.path.join(img_dir, "barrel.png"))

add_card(s4, Inches(6.8), Inches(1.8), Inches(5.7), Inches(5.1), "Responsive Platforming Feel",
         ["Coyote Time (0.12s): Jump remains valid for 120ms after leaving ledge.",
          "Jump Buffer (0.12s): Pre-landing space presses queue jump automatically.",
          "Dual Ground Detection: Composite box check + downward slope raycast.",
          "Strict Collision Isolation: Enemies and bombs ignored for ground check.",
          "Audio Cadence: Footstep sound plays rhythmically only while grounded.",
          "Impact Punchiness: Knockback impulse and sprite invulnerability flicker."],
         os.path.join(img_dir, "player.png"))

# ----------------- SLIDE 5: Unity Implementation -----------------
s5 = prs.slides.add_slide(blank_layout)
set_slide_background(s5)
add_header(s5, "Unity Architecture & Script Implementation", "04. Engineering")

add_card(s5, Inches(0.8), Inches(1.8), Inches(3.7), Inches(5.1), "Player & Combat",
         ["PlayerMovement.cs: Rigidbody2D velocity control, health events, bomb throw.",
          "IDamageable.cs: Decoupled damage interface implemented across Player & AI.",
          "Bomb.cs: Timer management, SpriteRenderer fuse charge, Physics2D blast.",
          "Ground Check: OverlapBox + Raycast avoiding tag collisions."],
         os.path.join(img_dir, "player.png"))

add_card(s5, Inches(4.8), Inches(1.8), Inches(3.7), Inches(5.1), "Managers & Flow",
         ["LevelManager.cs: Persistent singleton managing scene switches & restarts.",
          "LevelDoor.cs: Trigger & distance dual-sensing script loading next scene.",
          "MainMenuController.cs: Retro keyboard UI responding to Up/Down/Enter.",
          "EditorBuildSettings: Indices mapped (0: Menu, 1: Level1, 2: Level2)."],
         os.path.join(img_dir, "door_open.png"))

add_card(s5, Inches(8.8), Inches(1.8), Inches(3.7), Inches(5.1), "Audio & Interface",
         ["SoundManager.cs: Central audio engine with randomized clip variations.",
          "Dedicated 2D SFX channel and separate walking footstep channel.",
          "GameHUD.cs: Subscribes to player events, toggling active hearts.",
          "Clean Death Loop: Removed OnGUI blocking boxes for rapid replay."],
         os.path.join(img_dir, "health_bar.png"))

# ----------------- SLIDE 6: World & Level Design -----------------
s6 = prs.slides.add_slide(blank_layout)
set_slide_background(s6)
add_header(s6, "World and Level Design", "05. Level Architecture")

add_card(s6, Inches(0.8), Inches(1.8), Inches(5.6), Inches(5.1), "Level 1: The Interior Hold",
         ["Aesthetic: Warm wooden ship cabin with candle chandeliers and windows.",
          "Platforming: Gentle introductory elevation climb with wide jump landings.",
          "Pacing: Low-to-moderate threat density; introduces Bald Pirate & Big Guy.",
          "Exit Goal: Climb to top-right landing to enter Door at (6.45, 3.87).",
          "Lighting: Global 2D ambient + 2D Point Lights casting soft shadows.",
          "Hazard Zones: Fall boundary configured at y = -6.5."],
         os.path.join(img_dir, "gameplay.png"))

add_card(s6, Inches(6.8), Inches(1.8), Inches(5.7), Inches(5.1), "Level 2: The Upper Quarters",
         ["Aesthetic: Expansive multi-deck pirate fortress with vertical shafts.",
          "Platforming: Precision jumping over chasms, narrow wooden ledges.",
          "Enemy Gauntlet: Heavy combination of Cucumbers, Big Guys, Bald Pirates.",
          "Combat Depth: Requires utilizing high ground to drop bombs on patrol routes.",
          "Fall Danger: Wide gaps require mastering Coyote Time & Jump Buffering.",
          "Atmosphere: Volumetric light beams shining through decorative portholes."],
         os.path.join(img_dir, "door.png"))

# ----------------- SLIDE 7: Enemy & Boss System -----------------
s7 = prs.slides.add_slide(blank_layout)
set_slide_background(s7)
add_header(s7, "Enemy & Boss System", "06. Enemy AI")

add_card(s7, Inches(0.8), Inches(1.8), Inches(3.7), Inches(2.4), "Bald Pirate",
         ["Tactics: Kicks bombs away with high horizontal velocity.",
          "Durability: 1 bomb explosion.",
          "Counter: Drop bomb on higher ledge or time fuse."],
         os.path.join(img_dir, "bald_pirate.png"))

add_card(s7, Inches(4.8), Inches(1.8), Inches(3.7), Inches(2.4), "Big Guy (Heavy Boss)",
         ["Tactics: Picks up active bombs and throws them back at player!",
          "Durability: Takes 3 bombs to eliminate.",
          "Role: High-threat mini-boss requiring hit-and-run tactics."],
         os.path.join(img_dir, "big_guy.png"))

add_card(s7, Inches(8.8), Inches(1.8), Inches(3.7), Inches(2.4), "Cucumber",
         ["Tactics: Aggressive speed, charges player with melee bite.",
          "Durability: Takes 2 bombs to eliminate.",
          "Challenge: Fast approach forces rapid platform retreat."],
         os.path.join(img_dir, "cucumber.png"))

add_card(s7, Inches(0.8), Inches(4.5), Inches(5.6), Inches(2.4), "Captain (Cowardly Commander)",
         ["Behavior: High detection range; flees in panic when a bomb is nearby.",
          "Durability: 1 bomb explosion to defeat.",
          "Gameplay Dynamic: Cornering the captain while dodging other threats."],
         os.path.join(img_dir, "captain.png"))

add_card(s7, Inches(6.8), Inches(4.5), Inches(5.7), Inches(2.4), "Whale (Bomb Devourer)",
         ["Special Power: Literally swallows ignited bombs whole, nullifying them!",
          "Vulnerability: Immune to direct bombs; eliminated via fall hazards.",
          "Puzzle Element: Luring whale off platform edges."],
         os.path.join(img_dir, "whale.png"))

# ----------------- SLIDE 8: UI/HUD & Audio Design -----------------
s8 = prs.slides.add_slide(blank_layout)
set_slide_background(s8)
add_header(s8, "UI / HUD and Audio Design", "07. Sensory Experience")

add_card(s8, Inches(0.8), Inches(1.8), Inches(5.6), Inches(5.1), "Interface & HUD System",
         ["Health Bar HUD: Stylized pirate ribbon banner at top-left screen.",
          "Dynamic Hearts: 3 interactive heart sprites toggle on health change.",
          "Bomb Fuse Bar: Shows charging, full pulsing, and closing phases.",
          "Clean Death Experience: Removed all blocking black dialog boxes.",
          "Standalone Main Menu: Retro typography with selection cursor (>).",
          "Specification Compliance: Prominent 'Use UP / DOWN and ENTER' footer."],
         os.path.join(img_dir, "health_bar.png"))

add_card(s8, Inches(6.8), Inches(1.8), Inches(5.7), Inches(5.1), "Sound Effects Mapping",
         ["Walking: Gravel.ogg footstep audio rhythmically plays on ground movement.",
          "Bomb Initiation & Blast: 01_Explosion_v1..v3 trigger on spawn & detonation.",
          "Jump: 01_Jump_v1.ogg plays crisp jump launch sound.",
          "Damage & Hit: 01_Hurt_v1..v4 with slight random pitch modulation.",
          "Death: 01_Death_v1.ogg plays instant game over demise cue.",
          "Pit Fall: 01_Fall_v1.ogg plays cartoon descent whistle into chasms.",
          "Menu Navigation: Punch sound on arrow change, Start sound on Enter."],
         os.path.join(img_dir, "bomb.png"))

# ----------------- SLIDE 9: Outcomes Without Unity -----------------
s9 = prs.slides.add_slide(blank_layout)
set_slide_background(s9)
add_header(s9, "Outcomes Without Unity (Raw Custom Code)", "08. Comparative Analysis")

add_card(s9, Inches(0.8), Inches(1.8), Inches(3.7), Inches(5.1), "Low-Level Math & Physics",
         ["Must write custom SAT / AABB 2D collision resolution from scratch.",
          "Manual implementation of velocity integration, friction, and gravity.",
          "Risk of tunneling and clipping without continuous collision checks.",
          "Dozens of engineering hours wasted on basic physics bugs."],
         border_color=RED)

add_card(s9, Inches(4.8), Inches(1.8), Inches(3.7), Inches(5.1), "Graphics & Rendering Pipeline",
         ["Must write custom OpenGL / DirectX / Metal sprite batcher.",
          "Manual atlas slicing, UV calculation, and sprite matrix transforms.",
          "Complex shader programming required for 2D lighting and shadows.",
          "No visual scene hierarchy or visual camera viewport tools."],
         border_color=RED)

add_card(s9, Inches(8.8), Inches(1.8), Inches(3.7), Inches(5.1), "Animation & Audio Overhead",
         ["Custom state machine code required for frame timing and transitions.",
          "Direct integration with OS audio APIs (XAudio2, CoreAudio, ALSA).",
          "No visual tilemap painting; level data stored in manual text arrays.",
          "Severe development friction; 80% time spent on boilerplate."],
         border_color=RED)

# ----------------- SLIDE 10: Outcomes With Unity -----------------
s10 = prs.slides.add_slide(blank_layout)
set_slide_background(s10)
add_header(s10, "Outcomes With Unity (Engine Acceleration)", "09. Engine Advantages")

add_card(s10, Inches(0.8), Inches(1.8), Inches(3.7), Inches(5.1), "Turnkey Physics & Lighting",
         ["Robust Box2D integration (Rigidbody2D, CompositeCollider2D).",
          "URP 2D Light sources out-of-the-box (Point, Global, Freeform).",
          "LayerMask filtering for ground, enemy, and bomb layers.",
          "Instant visual debugging with Gizmos (check sizes, fall lines)."],
         border_color=GREEN)

add_card(s10, Inches(4.8), Inches(1.8), Inches(3.7), Inches(5.1), "Visual Workflow & Tooling",
         ["Interactive Tilemap palette for organic level design in minutes.",
          "Animator controllers with visual state graphs, parameters & blends.",
          "Prefab workflow: change Player or Bomb once, propagates everywhere.",
          "SceneManager API: instantaneous scene reloads and asynchronous jumps."],
         border_color=GREEN)

add_card(s10, Inches(8.8), Inches(1.8), Inches(3.7), Inches(5.0), "Focus on Gameplay Depth",
         ["Engine handled 95% of rendering, audio spatialization & physics.",
          "100% of engineering focused on unique enemy behaviors & game feel.",
          "Rapid iteration: polished coyote time, jump buffering & footstep timers.",
          "Cross-platform readiness: one codebase targets PC, WebGL & consoles."],
         border_color=GREEN)

# ----------------- SLIDE 11: Learning Outcomes -----------------
s11 = prs.slides.add_slide(blank_layout)
set_slide_background(s11)
add_header(s11, "Key Learning Outcomes & Takeaways", "10. Post-Mortem")

add_card(s11, Inches(0.8), Inches(1.8), Inches(3.7), Inches(5.1), "Architecture & C# Patterns",
         ["Event-Driven Design: Decoupling health changes and UI via C# Actions.",
          "Singleton Managers: Robust persistent controllers (LevelManager, SoundManager).",
          "Interface Polymorphism: IDamageable applied cleanly across player & AI.",
          "Defensive Programming: Automatic fallback searches prevent null references."],
         os.path.join(img_dir, "player.png"))

add_card(s11, Inches(4.8), Inches(1.8), Inches(3.7), Inches(5.1), "Game Feel & Platforming",
         ["Coyote Time & Input Buffering transform stiff jumps into fluid platforming.",
          "Footstep Cadence: Timer-based footsteps sound infinitely better than loops.",
          "Friction & Zero-Drift: Direct horizontal velocity creates tight arcade control.",
          "Instant Retry Loops: Fast respawns keep player immersion unbroken."],
         os.path.join(img_dir, "bomb.png"))

add_card(s11, Inches(8.8), Inches(1.8), Inches(3.7), Inches(5.1), "Iterative Problem Solving",
         ["Resolving internal namespace collisions (Unity internal AudioManager vs custom).",
          "Removing visual friction: eliminating disruptive UI boxes upon death.",
          "Multi-Scene Build Pipeline: Proper build indexing for standalone builds.",
          "Specification Rigor: Implementing exact menu controls per design document."],
         os.path.join(img_dir, "skull.png"))

output_pptx = r"c:\Users\ojasd\Downloads\CreateWithCode\TheBombGame\TheBombGame_Presentation.pptx"
output_pptx_dl = r"c:\Users\ojasd\Downloads\TheBombGame_Presentation.pptx"

prs.save(output_pptx)
prs.save(output_pptx_dl)
print(f"Successfully generated: {output_pptx}")
print(f"Successfully generated: {output_pptx_dl}")
