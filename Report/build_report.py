from pathlib import Path
from docx import Document
from docx.shared import Inches, Pt, RGBColor
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.enum.section import WD_SECTION
from docx.enum.table import WD_TABLE_ALIGNMENT, WD_CELL_VERTICAL_ALIGNMENT
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from PIL import Image, ImageDraw, ImageFont
import textwrap

ROOT=Path(r'C:\code\Space-game')
OUT=ROOT/'Report'
FIG=OUT/'figures'
OUT.mkdir(exist_ok=True); FIG.mkdir(exist_ok=True)
DOC=OUT/'Educational_Space_Game_Project_Report_Final.docx'

def font(size=22,bold=False):
    paths=[r'C:\Windows\Fonts\arial.ttf',r'C:\Windows\Fonts\arialbd.ttf']
    try: return ImageFont.truetype(paths[1 if bold else 0],size)
    except: return ImageFont.load_default()

def diagram(name,title,boxes,edges, W=1500,H=900):
    im=Image.new('RGB',(W,H),'white'); d=ImageDraw.Draw(im)
    titlef=font(34,True); head=font(23,True); body=font(19)
    d.text((W//2,26),title,font=titlef,fill='#122944',anchor='mt')
    centers={k:(x+w//2,y+h//2) for k,(x,y,w,h,txt) in boxes.items()}
    for a,b,label in edges:
        x1,y1=centers[a]; x2,y2=centers[b]
        d.line((x1,y1,x2,y2),fill='#566b82',width=4)
        import math
        ang=math.atan2(y2-y1,x2-x1); al=17
        p1=(x2-al*math.cos(ang-.5),y2-al*math.sin(ang-.5)); p2=(x2-al*math.cos(ang+.5),y2-al*math.sin(ang+.5))
        d.polygon([(x2,y2),p1,p2],fill='#566b82')
        if label: d.text(((x1+x2)//2,(y1+y2)//2-20),label,font=font(16),fill='#25384d',anchor='mm')
    for k,(x,y,w,h,txt) in boxes.items():
        d.rounded_rectangle((x,y,x+w,y+h),radius=18,fill='#edf4fb',outline='#24608d',width=3)
        lines=txt.split('\n'); yy=y+15
        for i,line in enumerate(lines):
            d.text((x+w//2,yy),line,font=head if i==0 else body,fill='#152e48',anchor='mt'); yy+=32
    im.save(FIG/name)

diagram('scene_flow.png','Verified scene flow',{
 'menu':(570,120,360,100,'Main menu'), 'missions':(570,340,360,110,'MissionSelect'), 'rocket':(570,570,360,110,'RocketLauncher'), 'space':(570,780,360,90,'SpaceScene'), 'academy':(1080,340,330,110,'SpaceAcademy')},
 [('menu','missions','Start mission'),('missions','rocket','Selected mission'),('rocket','space','Gate transition'),('menu','academy','Open Academy')],H=1000)
diagram('architecture.png','Runtime architecture based on current scripts',{
 'ui':(60,160,300,130,'Player UI\nSpaceShip / Mission UI'), 'workspace':(430,160,330,130,'BlockWorkspaceController\nBuild block sequence'), 'runner':(840,160,280,130,'ShipBlockRunner\nSequential coroutine'), 'blocks':(1160,160,290,130,'IShipBlock\n8 block kinds'),
 'rb':(840,430,280,120,'Rigidbody\nRocket motion'), 'fuel':(430,430,330,120,'RocketFuelSystem\nFuel state'), 'detect':(60,430,300,120,'SpeedDetection\nMission outcome'), 'trail':(1160,430,290,120,'TrajectoryTrail\nRecorded path'), 'mission':(60,700,300,120,'MissionData\nScriptableObject')},
 [('ui','workspace','Assemble'),('workspace','runner','List<IShipBlock>'),('runner','blocks','Execute'),('runner','rb','Forces / motion'),('runner','fuel','Consume'),('rb','detect','Position / speed'),('rb','trail','Positions'),('mission','detect','Threshold')],H=900)
diagram('use_cases.png','Student use cases supported by the project',{
 'actor':(50,380,260,100,'Student'), 'select':(480,70,360,100,'Choose mission'), 'code':(480,220,360,100,'Build block program'), 'launch':(480,370,360,100,'Launch / retry rocket'), 'monitor':(480,520,360,100,'Review flight and outcome'), 'explore':(480,670,360,100,'Explore solar system'), 'academy':(980,220,400,100,'Practice force and gravity'), 'progress':(980,430,400,100,'View local mastery progress')},
 [('actor','select',''),('actor','code',''),('actor','launch',''),('actor','monitor',''),('actor','explore',''),('actor','academy',''),('actor','progress','')],H=850)

doc=Document()
sec=doc.sections[0]; sec.page_width=Inches(8.27); sec.page_height=Inches(11.69); sec.left_margin=Inches(1.25); sec.right_margin=Inches(1); sec.top_margin=Inches(1); sec.bottom_margin=Inches(1)
sec.different_first_page_header_footer=True
styles=doc.styles
for s in ['Normal','Body Text']:
 st=styles[s]; st.font.name='Times New Roman'; st._element.rPr.rFonts.set(qn('w:eastAsia'),'Times New Roman'); st.font.size=Pt(12)
 st.paragraph_format.line_spacing=1.5; st.paragraph_format.space_after=Pt(6); st.paragraph_format.alignment=WD_ALIGN_PARAGRAPH.JUSTIFY
for nm,size in [('Title',22),('Heading 1',16),('Heading 2',14),('Heading 3',12)]:
 st=styles[nm]; st.font.name='Times New Roman'; st._element.rPr.rFonts.set(qn('w:eastAsia'),'Times New Roman'); st.font.size=Pt(size); st.font.bold=True; st.font.color.rgb=RGBColor(20,45,70); st.paragraph_format.keep_with_next=True
 st.paragraph_format.space_before=Pt(12); st.paragraph_format.space_after=Pt(6)

def shade(cell,color='DCEAF5'):
 tcPr=cell._tc.get_or_add_tcPr(); shd=OxmlElement('w:shd'); shd.set(qn('w:fill'),color); tcPr.append(shd)
def set_cell(cell,txt,bold=False,size=10):
 cell.text=''; p=cell.paragraphs[0]; p.paragraph_format.line_spacing=1.05; p.paragraph_format.space_after=Pt(2)
 r=p.add_run(str(txt)); r.bold=bold; r.font.name='Times New Roman'; r.font.size=Pt(size)
 cell.vertical_alignment=WD_CELL_VERTICAL_ALIGNMENT.CENTER
table_counter=-1
def table(headers,rows,widths=None):
 global table_counter
 table_counter += 1
 table_titles=[('3.1','Functional requirements and implementation status'),('3.2','Non-functional requirements and evidence status'),('3.3','Environment and software configuration'),('4.1','Scene inventory'),('4.2','Core script responsibilities'),('5.1','Missions and constraints'),('5.2','Block command semantics'),('6.1','Project folder organization'),('7.1','Evidence-led test record'),('7.2','Proposed functional test plan'),('8.1','Objective achievement'),('9.1','Limitations and future work')]
 if table_counter>1:
  label,title=table_titles[table_counter-2]
  p=doc.add_paragraph(); p.paragraph_format.keep_with_next=True; p.paragraph_format.space_before=Pt(5); p.paragraph_format.space_after=Pt(3)
  rr=p.add_run(f'Table {label} {title}'); rr.bold=True; rr.font.size=Pt(10)
 t=doc.add_table(rows=1,cols=len(headers)); t.style='Table Grid'; t.alignment=WD_TABLE_ALIGNMENT.CENTER
 for i,h in enumerate(headers): set_cell(t.rows[0].cells[i],h,True,10); shade(t.rows[0].cells[i])
 for row in rows:
  cells=t.add_row().cells
  for i,x in enumerate(row): set_cell(cells[i],x,size=9)
 if widths:
  for row in t.rows:
   for i,w in enumerate(widths): row.cells[i].width=Inches(w)
 doc.add_paragraph()
 return t
def simple_table(headers,rows,widths=None):
 t=doc.add_table(rows=1,cols=len(headers)); t.style='Table Grid'; t.alignment=WD_TABLE_ALIGNMENT.CENTER
 for i,h in enumerate(headers): set_cell(t.rows[0].cells[i],h,True,10); shade(t.rows[0].cells[i])
 for row in rows:
  cells=t.add_row().cells
  for i,x in enumerate(row): set_cell(cells[i],x,size=9)
 if widths:
  for row in t.rows:
   for i,w in enumerate(widths): row.cells[i].width=Inches(w)
 return t
def para(s='',boldlead=None):
 p=doc.add_paragraph()
 if boldlead and s.startswith(boldlead): p.add_run(boldlead).bold=True; p.add_run(s[len(boldlead):])
 else: p.add_run(s)
 return p
def bullet(s,level=0):
 p=doc.add_paragraph(style='List Bullet' if level==0 else 'List Bullet 2'); p.paragraph_format.line_spacing=1.15; p.paragraph_format.space_after=Pt(3); p.add_run(s); return p
def numbered(s):
 p=doc.add_paragraph(style='List Number'); p.paragraph_format.line_spacing=1.15; p.add_run(s); return p
def h1(s): doc.add_heading(s,1)
def h2(s): doc.add_heading(s,2)
def h3(s): doc.add_heading(s,3)
def caption(text):
 p=doc.add_paragraph(); p.alignment=WD_ALIGN_PARAGRAPH.CENTER; p.paragraph_format.keep_with_next=True; p.paragraph_format.space_before=Pt(4); p.paragraph_format.space_after=Pt(9)
 r=p.add_run(text); r.italic=True; r.font.size=Pt(10)
def figure(path,cap,width=6.0):
 p=doc.add_paragraph(); p.alignment=WD_ALIGN_PARAGRAPH.CENTER; p.add_run().add_picture(str(path),width=Inches(width)); caption(cap)
def pagebreak(): doc.add_page_break()
def field(run,code):
 begin=OxmlElement('w:fldChar'); begin.set(qn('w:fldCharType'),'begin'); ins=OxmlElement('w:instrText'); ins.set(qn('xml:space'),'preserve'); ins.text=code; sep=OxmlElement('w:fldChar'); sep.set(qn('w:fldCharType'),'separate'); txt=OxmlElement('w:t'); txt.text='Update field in Word to refresh'; end=OxmlElement('w:fldChar'); end.set(qn('w:fldCharType'),'end')
 for x in (begin,ins,sep,txt,end): run._r.append(x)

# Cover page (format 1_CoverPage.doc)
for _ in range(2): doc.add_paragraph()
p=doc.add_paragraph(); p.alignment=WD_ALIGN_PARAGRAPH.CENTER; r=p.add_run('CHAROTAR UNIVERSITY OF SCIENCE AND TECHNOLOGY'); r.bold=True; r.font.size=Pt(16)
p=doc.add_paragraph(); p.alignment=WD_ALIGN_PARAGRAPH.CENTER; p.add_run('Project Report\nOn').bold=True
p=doc.add_paragraph(); p.alignment=WD_ALIGN_PARAGRAPH.CENTER; r=p.add_run('EDUCATIONAL SPACE GAME'); r.bold=True; r.font.size=Pt(22)
p=doc.add_paragraph(); p.alignment=WD_ALIGN_PARAGRAPH.CENTER; p.add_run('Prepared by').bold=True
p=doc.add_paragraph(); p.alignment=WD_ALIGN_PARAGRAPH.CENTER; p.add_run('Khush Vaghani (24CE135)\nShreejay Shiyani (24CE117)')
p=doc.add_paragraph(); p.alignment=WD_ALIGN_PARAGRAPH.CENTER; p.add_run('Under the guidance of\nMartin Parmar')
p=doc.add_paragraph(); p.alignment=WD_ALIGN_PARAGRAPH.CENTER; p.add_run('Submitted to\nCharotar University of Science and Technology\nDegree of Bachelor of Technology in Computer Engineering\nCEUP301: Project-I | 5th Semester')
for _ in range(2): doc.add_paragraph()
p=doc.add_paragraph(); p.alignment=WD_ALIGN_PARAGRAPH.CENTER; p.add_run('U. & P. U. Patel Department of Computer Engineering\nFaculty of Technology & Engineering, CHARUSAT\nChandubhai S. Patel Institute of Technology\nChanga, Gujarat 388421').bold=True
p=doc.add_paragraph(); p.alignment=WD_ALIGN_PARAGRAPH.CENTER; p.add_run('November 2026')
pagebreak()
# First page (format 2_Firstpage.doc)
for _ in range(2): doc.add_paragraph()
p=doc.add_paragraph(); p.alignment=WD_ALIGN_PARAGRAPH.CENTER; p.add_run('Project Report\nOn').bold=True
p=doc.add_paragraph(); p.alignment=WD_ALIGN_PARAGRAPH.CENTER; r=p.add_run('EDUCATIONAL SPACE GAME'); r.bold=True; r.font.size=Pt(21)
p=doc.add_paragraph(); p.alignment=WD_ALIGN_PARAGRAPH.CENTER; p.add_run('(CEUP301 - Project-I)').bold=True
p=doc.add_paragraph(); p.alignment=WD_ALIGN_PARAGRAPH.CENTER; p.add_run('Prepared by\nKhush Vaghani (24CE135)\nShreejay Shiyani (24CE117)')
p=doc.add_paragraph(); p.alignment=WD_ALIGN_PARAGRAPH.CENTER; p.add_run('Under the supervision of\nMartin Parmar')
p=doc.add_paragraph(); p.alignment=WD_ALIGN_PARAGRAPH.CENTER; p.add_run('Submitted to\nCharotar University of Science and Technology (CHARUSAT)\nfor partial fulfillment of the requirements for the\nDegree of Bachelor of Technology (B.Tech.)\nin Computer Engineering, Semester 5')
for _ in range(2): doc.add_paragraph()
p=doc.add_paragraph(); p.alignment=WD_ALIGN_PARAGRAPH.CENTER; p.add_run('U. & P. U. Patel Department of Computer Engineering\nChandubhai S. Patel Institute of Technology (CSPIT)\nFaculty of Technology & Engineering (FTE), CHARUSAT\nChanga, Gujarat 388421').bold=True
p=doc.add_paragraph(); p.alignment=WD_ALIGN_PARAGRAPH.CENTER; p.add_run('November 2026')
pagebreak()
# Certificate page (based on 3_Certificate.doc; signatures remain for the institute)
h1('CERTIFICATE')
para('This is to certify that the report entitled "Educational Space Game" is a bona fide project report prepared by Khush Vaghani (24CE135) and Shreejay Shiyani (24CE117) under the guidance and supervision of Martin Parmar for CEUP301: Project-I of the 5th Semester of Bachelor of Technology in Computer Engineering at the Faculty of Technology & Engineering, CHARUSAT, Gujarat.')
para('To the best of the undersigned guide\'s knowledge and belief, this work has been completed by the candidates and fulfills the requirements of the B.Tech. degree ordinance. The report is submitted for institutional review and approval.')
for _ in range(2): doc.add_paragraph()
simple_table(['Under the supervision of','Head of Department'],[['Signature: __________________________\nMartin Parmar\nDesignation: ________________________\nU. & P. U. Patel Department of Computer Engineering\nCSPIT, CHARUSAT - Changa','Signature: __________________________\nName: ______________________________\nHead, U. & P. U. Patel Department of Computer Engineering\nCSPIT, CHARUSAT - Changa']], [3.0,3.0])
p=doc.add_paragraph(); p.alignment=WD_ALIGN_PARAGRAPH.CENTER; p.add_run('Chandubhai S. Patel Institute of Technology (CSPIT)\nFaculty of Technology & Engineering, CHARUSAT\nChanga, Gujarat 388421')
pagebreak()
# Candidate declaration (adapted from 4_Candidate declaration.docx to CEUP301 / Project-I)
h1('DECLARATION BY THE CANDIDATES')
para('We hereby declare that the project report entitled "Educational Space Game", submitted to Chandubhai S. Patel Institute of Technology, Changa, in partial fulfillment of the requirements for the award of the B.Tech. degree in Computer Engineering from the U. & P. U. Patel Department of Computer Engineering, CSPIT, FTE, CHARUSAT, is a record of bona fide CEUP301: Project-I work carried out by us under the guidance of Martin Parmar. We further declare that the work documented in this report has not been submitted, in whole or in part, for the award of any other degree or diploma at this or any other institute or university.')
for _ in range(2): doc.add_paragraph()
simple_table(['Candidate 1','Candidate 2'],[['Signature: __________________________\nKhush Vaghani\nStudent ID: 24CE135','Signature: __________________________\nShreejay Shiyani\nStudent ID: 24CE117']], [3.0,3.0])
para('Guide certification: I have reviewed the above declaration made by the candidates.')
para('Signature: __________________________\nMartin Parmar\nDesignation: ________________________\nU. & P. U. Patel Department of Computer Engineering, CSPIT, FTE, CHARUSAT - Changa')
pagebreak()
h1('ABSTRACT')
para('Educational Space Game is a Unity-based learning project that combines a block-programmed rocket mission with a solar-system scene and a separate Space Academy. In the mission flow, a student selects one of three landing challenges, assembles a sequence from visual programming blocks, and runs that sequence on a Rigidbody-driven rocket. A speed threshold at a configured altitude determines the displayed safe-landing or crash outcome. The space scene presents a simplified solar system and educational trajectory and orbital displays. Space Academy provides a worked force challenge, an adjustable F = m × a experiment, and a gravity comparison activity for Earth, Moon, and Mars.')
para('The project is implemented in Unity 6000.3.2f1 for a Windows 64-bit standalone target. The inspected build settings contain five scenes: Main Menu, MissionSelect, RocketLauncher, SpaceScene, and SpaceAcademy. Mission definitions are stored as ScriptableObject assets; rocket actions are represented by an IShipBlock coroutine interface, with nested conditional and repeat blocks. The orbital content uses simplified scripted motion and an educational Earth-relative model rather than a complete n-body simulation.')
para('The report is based on current project code, scene configuration, saved Unity screenshots, supplied planning material, and the university report-format documents. The developer team reports that functions were tested manually, and historical planning notes describe manual editor playtesting. No detailed test log, student evaluation, measured learning outcome, or automated test result was available for this review. Accordingly, the report describes implementation evidence and separates it from educational effectiveness claims.')
para('Keywords: educational game, Unity, visual programming, Newtonian mechanics, rocket simulation, STEM learning.')
pagebreak()
h1('ACKNOWLEDGEMENT')
para('The project team acknowledges the guidance of Martin Parmar. The report also draws on the project’s planning and implementation notes and the supplied Project-I report-format materials. The authors thank the academic evaluators for reviewing this work. Specific institutional approvals and certificate wording remain subject to the institute’s review.')
pagebreak()
h1('TABLE OF CONTENTS')
p=doc.add_paragraph(); field(p.add_run(), 'TOC \\o "1-3" \\h \\z \\u')
para('In Microsoft Word, update the field to generate or refresh page numbers and section entries.')
pagebreak()
h1('LIST OF FIGURES')
for x in ['Figure 4.1 Verified scene flow','Figure 4.2 Runtime architecture based on current scripts','Figure 4.3 Student use cases supported by the project','Figure 5.1 Mission selection screen','Figure 5.2 Block workspace screenshot (saved development capture)','Figure 5.3 Solar-system scene','Figure 5.4 Space Academy: challenge mode','Figure 5.5 Space Academy: force experiment','Figure 5.6 Space Academy: gravity lab']:
 para(x)
pagebreak(); h1('LIST OF TABLES')
for x in ['Table 3.1 Functional requirements and implementation status','Table 3.2 Non-functional requirements and evidence status','Table 3.3 Environment and software configuration','Table 4.1 Scene inventory','Table 4.2 Core script responsibilities','Table 5.1 Missions and constraints','Table 5.2 Block command semantics','Table 6.1 Project folder organization','Table 7.1 Evidence-led test record','Table 7.2 Proposed functional test plan','Table 8.1 Objective achievement','Table 9.1 Limitations and future work']:
 para(x)
pagebreak(); h1('ABBREVIATIONS AND GLOSSARY')
table(['Term','Meaning'],[['API','Application Programming Interface'],['C#','C-sharp programming language'],['F = m × a','Newton’s second law: force equals mass times acceleration'],['HUD','Heads-up display'],['IShipBlock','Project interface used by executable rocket command blocks'],['PlayerPrefs','Unity local key-value preference storage'],['Rigidbody','Unity physics component representing mass and motion'],['ScriptableObject','Unity asset type used to store reusable data independently of scene objects'],['STEM','Science, Technology, Engineering, and Mathematics'],['UI / UX','User interface / user experience'],['URP','Universal Render Pipeline'],['m/s','Metres per second; mission speed label used in project UI'],['μ','Gravitational parameter used in the simplified orbital calculations']])

# report sections
pagebreak(); h1('CHAPTER 1: INTRODUCTION')
h2('1.1 Background')
para('School learners often encounter mechanics as equations before they have a chance to experiment with how inputs affect motion. Educational games can make variables and feedback visible, but evidence from the wider field shows that benefits depend on instructional design and evaluation; a game should not be assumed to improve learning merely because it is interactive [1–3]. This project uses a space theme to connect basic physics ideas with a visual, hands-on activity.')
h2('1.2 Problem statement')
para('The project addresses the design challenge of presenting introductory force, acceleration, velocity, gravity, and orbital ideas through an interactive computer game. The available planning notes describe a desire to let a learner reason about when to act and how much force to apply, with failure giving useful feedback. The project does not claim to solve a measured educational deficiency; no baseline study or school needs analysis was provided.')
h2('1.3 Motivation')
para('A rocket and planetary setting provides an intuitive context for changing thrust, observing motion, and comparing gravitational environments. Visual programming blocks can expose sequence and control-flow concepts without requiring learners to write syntax. A separate Academy gives the student a place to manipulate numerical variables and see formula-driven feedback.')
h2('1.4 Project objectives')
for x in ['Create a Unity game flow with mission selection and a rocket-programming activity.','Let learners build and run an ordered sequence of visual commands, including basic control flow.','Connect command choices to rocket movement, fuel consumption, trajectory feedback, and mission outcome.','Provide a separate Space Academy with a force challenge, adjustable force experiment, and gravity comparison.','Present a simplified solar-system environment with planet information and orbital/trajectory visualization.','Support iteration through retry and local mastery progress.']:
 bullet(x)
h2('1.5 Scope')
para('The current project includes the five scenes listed in Chapter 4, three mission data assets, eight command-block kinds, a simplified orbital exploration scene, and three Academy modes. The project is a Windows 64-bit Unity application in its configured build target. No account system, teacher dashboard, learning analytics service, networking, formal classroom deployment, or school assessment integration was found. These are outside the verified implementation scope.')
h2('1.6 Audience and intended learning outcomes')
para('The intended audience, supplied by the project team, is school students. No exact grade band, age range, curriculum alignment, or reading-level specification was provided. The activity is designed to give practice with sequencing, conditionals, repetition, force and acceleration, speed thresholds, gravity comparisons, and qualitative orbital ideas. These are intended learning opportunities, not demonstrated learning gains.')
h2('1.7 Significance and evidence boundary')
para('The project brings together visual coding and mechanics in one game concept. Its value for student learning remains to be evaluated with learners, an appropriate comparison or pre/post measure, and teacher feedback. The current report describes functionality and educational rationale; it does not assert verified learning effectiveness.')

pagebreak(); h1('CHAPTER 2: EXISTING SYSTEMS AND LITERATURE REVIEW')
h2('2.1 Educational games and learning evidence')
para('Meta-analyses report positive average learning effects for serious games in some settings, while also finding variation by comparison condition, instructional support, and game design [1]. Reviews of game-based learning in primary education likewise show a growing evidence base but do not justify assuming that every game will produce learning gains [2]. Gamification research reports context-sensitive effects and highlights the importance of how game mechanics are integrated into instruction [3].')
para('The project therefore treats immediate feedback, experimentation, and meaningful learner decisions as design mechanisms to test, not as proof of educational impact. A future classroom evaluation should measure whether learners can transfer ideas to new physics problems and should collect usability and engagement feedback separately.')
h2('2.2 Related approaches')
para('NASA Space Place provides space-themed games and activities for school-age audiences, while NASA’s interactive resources include visual exploration tools for solar-system objects and missions [4, 5]. These demonstrate that space content can support informal learning and exploration. The present project’s distinguishing design direction is the combination of a block-programmed rocket challenge with a dedicated force/gravity laboratory. This is a feature-level comparison, not a claim of superiority; the products have different scope and maturity.')
h2('2.3 Visual programming in K–12')
para('Block-based programming can lower syntax barriers and provide a visual representation of sequence and control structures. Recent synthesis work examines learning outcomes across K–12 block-based programming studies, while also showing why outcomes should be measured rather than presumed [6]. In this project, Launch, Boost, Wait, Rotate, Orbit, Stop, If, and Repeat map visible commands to code execution. The project has no supplied curriculum mapping or independent learning study.')
h2('2.4 Gap and rationale')
para('The project’s opportunity is to couple a physics-themed task with the learner-authored command sequence: students choose actions, observe motion and feedback, then revise the sequence. Space Academy offers a more direct relationship between formula inputs and displayed values. A future study should determine whether this combination improves understanding beyond exposure to the interface alone.')
h2('2.5 Literature limits')
para('The cited sources provide general context, not evidence about this specific implementation. No user research, requirements survey, school pilot, or comparison against a named competing product was found in the accessible project materials.')

pagebreak(); h1('CHAPTER 3: REQUIREMENTS ANALYSIS')
h2('3.1 Stakeholders and user roles')
para('The primary role is the student player. The inspected project does not implement separate teacher, administrator, or account roles. The development team and academic supervisor are project stakeholders, not in-game roles.')
table(['ID','Functional requirement','Current evidence/status'],[
('FR-01','Navigate from main menu to mission selection and Space Academy.','Implemented in scene flow; five scenes in build settings.'),('FR-02','Choose among three landing missions with different speed thresholds.','Implemented using MissionData assets and MissionCardUI.'),('FR-03','Create, reorder, and remove commands in a visual workspace.','Implemented by BlockWorkspaceController; current scene contains block palette/workspace.'),('FR-04','Execute sequential commands and nested If/Repeat control flow.','Code implemented; runtime behaviour reported manually tested by developers, no test log.'),('FR-05','Apply propulsion, wait, rotate, orbit, stop, and fuel rules.','Implemented across block classes and RocketFuelSystem; no per-case formal results provided.'),('FR-06','Show trajectory and mission outcome, allow retry.','Implemented by trail, SpeedDetection, status UI and retry path.'),('FR-07','Explore a solar-system scene and planet details.','Scene and scripts present; saved screenshot evidence.'),('FR-08','Practice force, experiment with mass/acceleration, compare gravity.','Three Academy modes present; challenge is one fixed question.'),('FR-09','Save local Academy mastery/progress.','PlayerPrefs code present; persistence across restart not independently verified here.')])
h2('3.2 Non-functional requirements')
table(['ID','Quality concern','Evidence / status'],[
('NFR-01','Platform compatibility','Build target is StandaloneWindows64; packaged executable was not available for independent install verification.'),('NFR-02','Usability for school students','Intended audience is known; no age-band usability study or accessibility audit supplied.'),('NFR-03','Reliability of gameplay','Developer-reported manual testing; zero missing references in inspected scenes and zero Unity compile errors at inspection. No formal test suite results.'),('NFR-04','Educational validity','Conceptual alignment with mechanics; no student learning assessment or curriculum validation.'),('NFR-05','Performance','No frame-rate, memory, loading-time, or hardware benchmark available.'),('NFR-06','Maintainability','Features are separated into scenes/scripts and mission data assets; no code quality metrics available.'),('NFR-07','Accessibility/localization','No verified screen-reader support, remapping, captions, language options, or accessibility audit.')])
h2('3.3 Hardware and software environment')
table(['Item','Verified configuration'],[['Engine','Unity 6000.3.2f1'],['Language','C#'],['Render pipeline','Universal Render Pipeline (URP)'],['UI','Unity UI (UGUI) and TextMeshPro'],['Input package','Input System 1.17.0 is included; project also emits a warning about deprecated legacy Input Manager setting'],['Other packages','Cinemachine 3.1.7; Splines 2.8.4; Unity Test Framework 1.6.0 (package presence is not evidence that tests were run)'],['Build target','Standalone Windows 64-bit'],['Minimum end-user hardware','Not specified or benchmarked; cannot be responsibly stated from project files']])
h2('3.4 Assumptions and constraints')
for x in ['A learner can operate mouse-driven UI and understand the text labels; this has not been validated for a defined age group.','Mission speed values are presented as metres per second, while positions and physics remain Unity world units unless explicitly scaled.','Orbital constants and scene scale are educational abstractions, not calibrated astronomical data.','The application is configured for desktop Windows; platform support beyond the configured target is unverified.','No formal schedule dates, cost estimate, or individual task allocation were supplied.']:
 bullet(x)
h2('3.5 Feasibility')
para('Technical feasibility is evidenced by an openable Unity project with configured scenes and scripts, and by the Unity editor reporting no compile errors during inspection. Operational feasibility for a classroom remains unknown because no packaged build, device benchmark, teacher workflow, or student study was supplied. Economic feasibility cannot be assessed because development and deployment costs were not documented.')

pagebreak(); h1('CHAPTER 4: SYSTEM DESIGN AND ARCHITECTURE')
h2('4.1 Scene flow')
para('The build configuration includes Main Menu, MissionSelect, RocketLauncher, SpaceScene, and SpaceAcademy. MissionSelectManager stores the chosen MissionData in a static selection before loading RocketLauncher. LaunchAltitudeTrigger transitions a ship-tagged rocket into SpaceScene when it enters the gate. Space Academy is reachable from the menu as a separate learning path.')
figure(FIG/'scene_flow.png','Figure 4.1 Verified scene flow')
h2('4.2 Runtime architecture')
para('The rocket programming path is UI-driven. BlockWorkspaceController converts visible block entries into IShipBlock objects. ShipBlockRunner executes the resulting list sequentially as coroutines. Individual command classes manipulate a Rigidbody or invoke a nested sequence. RocketFuelSystem provides fuel state. SpeedDetection reads the mission data, rocket position, and velocity to determine the outcome; TrajectoryTrail records positions for visual feedback.')
figure(FIG/'architecture.png','Figure 4.2 Runtime architecture based on current scripts',6.3)
h2('4.3 Student use cases')
figure(FIG/'use_cases.png','Figure 4.3 Student use cases supported by the project',6.2)
h2('4.4 Scene inventory')
table(['Scene','Main purpose','Observed core objects/components'],[
('Main Menu','Entry and navigation','MainMenuNavigation, MainMenuProgressUI, MainMenuSecondaryNavigation, settings UI'),('MissionSelect','Mission choice','MissionSelectManager, three MissionCardUI instances'),('RocketLauncher','Program and launch rocket','SpaceShip, ShipBlockRunner, RocketFuelSystem, block palette/workspace, mission/fuel/status UI, SpaceGate'),('SpaceScene','Solar system and flight display','PlanetOrbit components, orbit paths, EarthLandingDetector, EarthOrbitalGravity, TrajectoryPredictor/Visualizer, CruiseController'),('SpaceAcademy','Guided learning activities','SpaceAcademyController, AcademyModeManager, ExperimentModeController, GravityLabController, TMP text UI')])
h2('4.5 Core script responsibilities')
table(['Script / type','Responsibility in current implementation'],[
('MissionData','Stores mission title/difficulty, threshold and target parameters as reusable asset data.'),('MissionSelectManager','Selects mission asset and loads RocketLauncher.'),('BlockWorkspaceController','Manages drag/drop, ordering, deletion, nested workspace UI, and creates command objects.'),('IShipBlock and block classes','Represent executable actions and control flow as coroutine-compatible commands.'),('ShipBlockRunner','Runs command sequence, reports progress/failure/end reason, and guards against overlapping runs.'),('RocketFuelSystem','Tracks fuel; Launch costs 20 and Boost costs 10 from a 100-unit starting store in the inspected configuration.'),('SpeedDetection','Checks downward crossing of configured altitude and compares speed with mission threshold.'),('SpaceRocketManager','Prepares the rocket for SpaceScene, scales and places it, assigns Earth-relative circular handoff velocity, and disables global gravity.'),('EarthOrbitalGravity','Applies simplified Earth-relative orbital acceleration and calculates telemetry-related state.'),('SpaceAcademy controllers','Provide fixed challenge, adjustable F=ma experiment, gravity comparison, tutorial and local progress UI.')])
h2('4.6 Data and persistence')
para('Mission configuration is stored in ScriptableObject assets under Assets/Mission. The selected mission is passed between scenes using a static field, so it is not a database record and is not designed for multi-session user accounts. Academy completion and selected settings use Unity PlayerPrefs, a local key-value store. Cloud synchronization, privacy controls, profiles, and teacher access are not implemented in the inspected code.')
h2('4.7 State and error handling')
para('ShipBlockRunner tracks completion count and total count, prevents a second run from starting while one is active, and records completion, Stop block termination, out-of-fuel, or fault end states. A conditional evaluates its selected sensor and comparator when execution reaches that block; if true, it executes its nested commands. Repeat executes its nested commands a configured integer number of times. The runner’s isolation behaviour should be covered by explicit tests because no detailed case-by-case results were supplied.')

pagebreak(); h1('CHAPTER 5: GAME DESIGN AND EDUCATIONAL EXPERIENCE')
h2('5.1 Game concept and loop')
para('The core mission loop is: choose a mission, assemble a program, start the countdown, observe rocket motion and fuel/trajectory feedback, review the mission result, and retry with a revised sequence. The Space Academy provides a separate loop of selecting a mode, changing inputs or answering a prompt, viewing feedback, and earning local completion/score progress.')
h2('5.2 Missions and constraints')
table(['Mission','Difficulty','Maximum landing speed','Target altitude','Horizontal distance'],[
('First Flight','Easy','20 m/s','70','26.07'),('Controlled Landing','Medium','15 m/s','70','26.07'),('Precision Landing','Hard','10 m/s','70','26.07')])
para('The configured mission assets share target height 70, horizontal distance 26.07, and gravity value 9.81. The outcome script checks total speed magnitude at the downward altitude crossing. These values and the threshold logic are part of the present design; they should not be interpreted as validated physical distances unless the project’s world-unit scale is separately specified.')
figure(ROOT/'Assets'/'Screenshots'/'GameView_20261013_203904.png','Figure 5.1 Mission selection screen (saved Unity Game View capture)')
h2('5.3 Visual programming commands')
table(['Block','Observed semantics'],[
('Launch','Enables gravity and applies launch force; consumes launch fuel.'),('Boost','Applies force along rocket local up direction and consumes fuel.'),('Wait','Yields for a duration.'),('Rotate / Turn','Changes Rigidbody rotation and rotates velocity.'),('Orbit','Runs a scripted circular movement via MovePosition with gravity temporarily disabled; not a natural gravitational orbit.'),('Stop','Sets velocity to zero and stops the current sequence.'),('If','Tests altitude, speed, or vertical speed with a selected comparator; runs nested commands when true.'),('Repeat','Runs nested commands a configured number of times.')])
para('The screenshot below is an earlier saved development capture with six visible basic commands. The current scene file also contains If and Repeat palette entries; the image should not be read as a complete current palette inventory.')
figure(ROOT/'Assets'/'Screenshots'/'dnd_test1.png','Figure 5.2 Block workspace screenshot (saved development capture; earlier six-command palette)')
h2('5.4 Solar-system exploration')
para('The SpaceScene presents the Sun, eight planets and Moon, scripted orbital paths, an asteroid belt, stars, labels, and planet information. PlanetOrbit components animate positions/rotation along simplified paths. EarthOrbitalGravity supplies an Earth-relative approximate gravity model; trajectory prediction and visualization show a short-horizon predicted path and markers. The system is intentionally simplified and is not a high-fidelity n-body solver.')
figure(ROOT/'Assets'/'Screenshots'/'GameView_20260830_113707.png','Figure 5.3 Solar-system scene (saved Unity Game View capture)')
h2('5.5 Space Academy')
para('Challenge mode asks one fixed Newton’s second law question: for mass 10 kg and acceleration 3 m/s², calculate force 30 N. The accepted value allows ±0.1 N. A hint reduces the awarded score from 10 to 5 points; streak and feedback UI are present. This is a single challenge, not a full question bank.')
figure(ROOT/'Assets'/'Screenshots'/'GameView_20261013_174755.png','Figure 5.4 Space Academy: challenge mode (saved screen showing 30 N response)')
para('Experiment mode exposes mass and acceleration sliders and displays F = m × a. The saved screen shows 20 kg and 4 m/s² producing 80 N, with an animated rocket. The visual response is primarily a presentation of formula inputs, not a measured rigidbody experiment.')
figure(ROOT/'Assets'/'Screenshots'/'GameView_20261013_175006.png','Figure 5.5 Space Academy: force experiment (saved screen showing 20 kg × 4 m/s² = 80 N)')
para('Gravity Lab offers Earth, Moon, and Mars presets (9.81, 1.62, and 3.71 m/s²), a mass control, and the relationship W = m × g. A 120 N lift capacity is used for illustrative crate feedback/animation. The visual is educational feedback rather than a physical load simulation.')
figure(ROOT/'Assets'/'Screenshots'/'GameView_20261013_175118.png','Figure 5.6 Space Academy: gravity lab (saved screen showing Earth weight example)')
h2('5.6 Feedback, rewards, and usability')
para('The rocket path provides countdown, fuel, block progress, speed/trajectory cues, outcome status, and retry. Academy modes provide hints, correctness feedback, score/streak, tutorial overlays, and local mastery indicators. Usability for school students, readability at different display sizes, accessibility, and learning transfer have not been independently evaluated. Saved screenshots evidence selected visual states only.')

pagebreak(); h1('CHAPTER 6: TECHNOLOGY STACK AND IMPLEMENTATION')
h2('6.1 Platform and packages')
para('The project is authored in Unity 6000.3.2f1 and C#. It uses URP, UGUI, TextMeshPro, Unity Rigidbody physics for the mission rocket, the Input System package, Cinemachine, and Splines. The project targets Windows 64-bit. Package presence is reported as configuration evidence, not as proof that every package is exercised in gameplay.')
h2('6.2 Folder organization')
table(['Path','Observed content'],[
('Assets/Scenes','The five gameplay scenes and related Unity scene files.'),('Assets/Scripts','Gameplay, UI, mission, block, solar-system, and Academy C# scripts.'),('Assets/Mission','Three mission ScriptableObject assets.'),('Assets/Screenshots','Saved Game View and editor captures used as visual project evidence.'),('Assets/UI','UI artwork and associated assets.'),('Packages / ProjectSettings','Unity package manifest and project configuration.')])
h2('6.3 Mission command execution')
para('The block workspace builds a List<IShipBlock>. Each block executes through an asynchronous coroutine contract. ShipBlockRunner advances through the list and publishes progress and end status. If and Repeat contain nested blocks. Launch and Boost apply Rigidbody forces; fuel is deducted from RocketFuelSystem. A 100-unit fuel store is configured, with a 20-unit launch cost and 10-unit boost cost; continuous per-second consumption is set to zero in the inspected setup.')
h2('6.4 Mission outcome algorithm')
para('SpeedDetection monitors the rocket’s vertical position relative to a configured altitude. When it detects a downward crossing, it compares the magnitude of the Rigidbody velocity with the selected mission’s speed threshold. Under the threshold, it stops the rocket and disables gravity; otherwise the crash/failure path is displayed. Retry resets relevant position, velocity, fuel, landing check, and trail state. This models a speed-gated landing result rather than collision with a modeled ground surface.')
h2('6.5 Space-scene handoff and orbital model')
para('When the gate is entered, the scene transition preserves the rocket. SpaceRocketManager scales the craft to 0.0125, positions it relative to an Earth spawn anchor, aligns its orientation, and gives it an Earth-relative circular handoff speed based on sqrt(μ/r), with μ configured as 900 and Earth’s scripted orbital velocity added. Global gravity is disabled for this space scene; EarthOrbitalGravity applies its local educational acceleration. This is a simplified model with chosen units and scope, not a physically scaled solar-system simulation.')
h2('6.6 Planet and Academy systems')
para('Planet motion is represented by scripted circular paths and transform updates. Trajectory prediction integrates a short local state estimate for visualization. In Academy, force and weight displays are formula-driven; gravity and experiment animations use transforms/visual effects rather than full physical simulations. PlayerPrefs stores selected progress locally.')
h2('6.7 Important implementation limitations')
for x in ['SpaceScene Run is a resume action when paused; it does not initiate a newly authored block sequence there. Reset cancels the current block sequence and resumes time but does not reset rocket pose.','The Orbit block is a scripted circular movement and temporarily disables gravity.','Landing success is determined by a configured altitude crossing and speed, not collision-based touchdown.','Solar-system paths and Earth-relative gravity use simplified, project-specific parameters.','The input configuration produces a legacy Input Manager deprecation warning. The Input System package is installed, but migration status should be confirmed.']:
 bullet(x)

pagebreak(); h1('CHAPTER 7: TESTING AND VALIDATION')
h2('7.1 Evidence available')
para('The project team reports that functions have been tested by the developers. The planning notes describe manual Unity Editor playtesting and iterative debugging. Saved screenshots show selected mission, Academy, and solar-system presentation states. During this review, Unity reported zero compilation errors and the five key scenes had no missing serialized references in the inspected scan. These checks do not establish that every runtime path works or that student learning outcomes are achieved.')
h2('7.2 Test evidence record')
table(['Evidence item','Observed/reported result','Interpretation'],[
('Developer report supplied in conversation','Team says functions were tested.','Manual testing is reported; no cases, dates, tester names, or results log provided.'),('Planning notes','Historical manual Editor playtesting and iteration described.','Historical process evidence; some notes predate current code and are not a current test matrix.'),('Unity editor inspection','Zero compile errors at inspection; deprecated Input Manager warning.','Compilation state only; not gameplay acceptance testing.'),('Scene reference scan','No missing serialized references found in the five key scenes.','Structural check only.'),('Saved screenshots','Mission selection and selected Academy/Solar System screens visible.','Evidence of those captured visual states; not proof of broad functionality.'),('Automated tests','Unity Test Framework package exists.','No test execution results or coverage report supplied.'),('Student evaluation','None supplied.','Learning effectiveness and school usability remain unvalidated.')])
h2('7.3 Proposed test plan — not run as part of this report')
table(['ID','Test action','Expected result to verify','Evidence to record'],[
('T-01','Open each enabled scene from build settings.','Scene loads without missing references or console exceptions.','Unity version, console log, scene name.'),('T-02','Select each mission card and start.','Correct mission values reach RocketLauncher.','Mission asset, selected UI, captured values.'),('T-03','Build, reorder, and delete command blocks.','Workspace order matches execution order; deleted blocks do not run.','Screen recording and sequence log.'),('T-04','Run each basic block and both nested control-flow blocks.','Force, wait, rotation, orbit, stop, condition, and repeat follow documented semantics.','Per-case inputs, event log, resulting motion.'),('T-05','Exhaust fuel during Launch/Boost.','Out-of-fuel end state and UI feedback are consistent.','Fuel before/after and runner status.'),('T-06','Cross mission altitude at speed above/below threshold.','Outcome follows selected mission threshold and retry resets the run.','Velocity, altitude, mission, outcome.'),('T-07','Pause, resume, and reset in SpaceScene.','Observed state matches intended semantics; reset behaviour clarified.','State before/after and console output.'),('T-08','Exercise Academy formulas and persistence.','F=m×a and W=m×g outputs are correct; progress persistence is confirmed.','Input/output matrix, restart test.'),('T-09','Conduct school-student usability and learning pilot.','Age-appropriate usability and learning outcomes are assessed.','Consent/ethics as applicable, rubric, pre/post results.')])
h2('7.4 Test limitations')
para('The report does not invent pass/fail results for proposed tests. No formal test plan, detailed defects list, regression record, automated test report, build artifact, performance benchmark, or student study was available. Historical planning documents mention earlier interface states that have since changed; current code and scenes were treated as more authoritative.')

pagebreak(); h1('CHAPTER 8: RESULTS AND DISCUSSION')
h2('8.1 Implemented outcomes')
para('Inspection confirms a five-scene Unity project with a mission selector, a block-based rocket programming scene, a solar-system scene, and a three-mode Space Academy. Three mission assets define increasing speed constraints. Current rocket programming code supports eight command kinds including nested If and Repeat. The captured screenshots show the mission selector and selected Academy and solar-system presentation states.')
h2('8.2 Objective achievement')
table(['Objective','Assessment based on available evidence'],[
('Provide mission selection and rocket program flow','Implemented in scene configuration and related scripts.'),('Represent block sequence and control flow','Current code and scene data include eight kinds; complete behaviour requires a documented runtime test matrix.'),('Connect actions with physics and feedback','Rigidbody, fuel, trajectory and outcome components are present; developer manual testing is reported without detailed logs.'),('Provide Space Academy concepts','Challenge, formula experiment, gravity comparison, and screenshots are present.'),('Explore solar-system concepts','Planet scene, orbit/trajectory components and screenshot are present; simulation is simplified.'),('Demonstrate learning gains','Not demonstrated; no student evaluation or learning measure supplied.')])
h2('8.3 Discussion')
para('The architecture provides a coherent educational prototype: commands are learner-authored, motion feedback can be observed, and separate activities expose elementary mechanics formulas. Its most important limitation for academic claims is lack of validation evidence. A successful scene capture or a developer’s manual test does not show that a learner understands the intended concept. Future evaluation should separate software correctness, usability, engagement, and learning transfer.')
h2('8.4 Evidence figures')
para('Figures 5.1–5.6 reproduce saved Unity Game View images from the project repository. They are historical captures, not freshly generated screenshots during this report review. Each figure is referenced in its explanatory section. No generic internet image is used as a substitute for project evidence.')

pagebreak(); h1('CHAPTER 9: CHALLENGES, LIMITATIONS, AND FUTURE SCOPE')
h2('9.1 Development approach and challenges')
para('The planning notes describe staged prototyping: begin with a vertical motion/landing loop, add horizontal motion and mission thresholds, add trajectory/detail feedback to reduce blind guessing, prototype force over time, and later add block programming. This is best described as iterative prototyping; the documents do not identify a formal Agile or Waterfall process. The notes also describe manual playtesting and debugging. Calendar dates, milestone acceptance criteria, and individual student task allocation were not documented.')
h2('9.2 Current limitations')
table(['Area','Current limitation'],[
('Educational scope','No specified grade band, curriculum alignment, teacher materials, student study, or measured learning outcomes.'),('Challenge content','Challenge mode contains one fixed problem; limited question variety and adaptive difficulty.'),('Physics fidelity','Orbital system uses scripted circular motion and a simplified Earth-relative gravity model; units/scale are not fully physical.'),('Landing model','Outcome is threshold crossing plus speed comparison, not a collision-based landing surface or impact model.'),('SpaceScene controls','Run resumes from pause; Reset does not restore rocket pose.'),('Validation','No detailed manual test logs, automated results, performance metrics, or packaged build verification.'),('Usability/accessibility','No age-specific or accessibility evaluation, localization, or alternate input audit.'),('Data','Player progress is local; no account, export, or teacher dashboard.')])
h2('9.3 Future work')
for x in ['Define grade bands, curriculum outcomes, and educator guidance with school partners.','Expand Academy question bank and add staged difficulty, explanations, and new problem types.','Add a real ground collider/landing model and clearly explain the difference between educational units and SI units.','Clarify and test SpaceScene pause, resume, and reset behaviour.','Add reproducible automated tests for mission asset selection, block semantics, fuel use, thresholds, and persistence.','Run usability testing with the intended age group and a carefully designed learning evaluation.','Add accessibility options such as scalable text, contrast settings, and input remapping after learner review.','Measure Windows performance on defined hardware and provide a packaged installation build.','Review orbital model assumptions and provide learner-facing explanations of its limits.']:
 bullet(x)

pagebreak(); h1('CHAPTER 10: CONCLUSION')
para('Educational Space Game is an implemented Unity prototype that combines a block-programmed rocket mission, a simplified solar-system environment, and a Space Academy for introductory force and gravity ideas. The current code and scenes support a substantial set of gameplay systems, including three mission assets, eight block command types, mission feedback, orbital visualization, and local learning-mode progress.')
para('The project objectives related to building these software features are supported by current project evidence. The available evidence does not establish that the game improves student learning, is validated for a particular school grade, or passes a formal test suite. The most responsible next step is to complete documented functional verification and then evaluate usability and learning outcomes with the intended students. Certificate and declaration pages should be appended only after the institute approves their final wording.')

pagebreak(); h1('REFERENCES')
refs=[
'[1] P. Wouters, C. van Nimwegen, H. van Oostendorp, and E. van der Spek, “A Meta-Analysis of the Cognitive and Motivational Effects of Serious Games,” Journal of Educational Psychology, vol. 105, no. 2, pp. 249–265, 2013. https://doi.org/10.1037/a0031311',
'[2] T. Hainey et al., “A systematic literature review of games-based learning empirical evidence in primary education,” Computers & Education, vol. 102, pp. 202–223, 2016. https://doi.org/10.1016/j.compedu.2016.09.001',
'[3] M. Sailer and L. Homner, “The Gamification of Learning: a Meta-analysis,” Educational Psychology Review, vol. 32, pp. 77–112, 2020. https://doi.org/10.1007/s10648-019-09498-w',
'[4] NASA, “NASA Space Place: Games.” https://spaceplace.nasa.gov/en/menu/games/ (accessed 27 Oct. 2026).',
'[5] NASA, “NASA Interactives.” https://www.nasa.gov/interactives/ (accessed 27 Oct. 2026).',
'[6] Q. Yu, K. Yu, and B. Li, “Effects of Block-Based Visual Programming on K-12 Students’ Learning Outcomes,” Journal of Educational Computing Research, published online 2024. https://doi.org/10.1177/07356331241293163',
'[7] Unity Technologies, “ScriptableObject,” Unity User Manual. https://docs.unity3d.com/6000.1/Documentation/Manual/class-ScriptableObject.html (accessed 27 Oct. 2026).',
'[8] Unity Technologies, “Rigidbody.AddForce,” Unity Scripting API. https://docs.unity3d.com/kr/current/ScriptReference/Rigidbody.AddForce.html (accessed 27 Oct. 2026).',
'[9] Unity Technologies, “Input System,” Unity Manual. https://docs.unity3d.com/kr/6000.0/Manual/com.unity.inputsystem.html (accessed 27 Oct. 2026).',
'[10] Educational Space Game project source, Unity scenes, scripts, mission assets, and screenshots, inspected 27 Oct. 2026.',
'[11] Educational Space Game project planning notes, “space-game-technical-doc.md,” supplied path C:\\gemdev\\SpaceGame, consulted 27 Oct. 2026.',
'[12] Project format materials supplied by the team: 1_CoverPage.doc, 2_Firstpage.doc, 3_Certificate.doc, 4_Candidate declaration.docx, and 5_Report Format-Development.doc, consulted 27 Oct. 2026.'
]
for r in refs:
 p=doc.add_paragraph(); p.paragraph_format.left_indent=Inches(.25); p.paragraph_format.first_line_indent=Inches(-.25); p.paragraph_format.line_spacing=1.1; p.paragraph_format.space_after=Pt(7); p.add_run(r)

pagebreak(); h1('APPENDIX A: STUDENT QUICK START')
numbered('Open the project using Unity 6000.3.2f1 in Unity Hub. The project targets Standalone Windows 64-bit.')
numbered('Open the Main Menu scene and run the project in the Unity Editor, or use an approved packaged Windows build if one is produced by the team.')
numbered('Choose a mission, arrange command blocks in the workspace, and use the launch control. Observe status, fuel, trajectory, and outcome feedback; use Retry to revise the program.')
numbered('Use the Space Academy menu to explore the force challenge, formula experiment, and gravity comparison.')
para('This is a project-level quick-start description, not a validated end-user manual. Exact control labels and supported deployment steps should be checked against the final packaged build.')
h1('APPENDIX B: REQUIREMENTS AND EVIDENCE NOTES')
para('The source of project claims is current Unity scenes, scripts, asset files and configuration unless an item is explicitly attributed to planning notes or the project team. Older planning notes and the supplied implementation PDF describe earlier interface or physics states; where they conflict with current source, current source was used. The saved image captures are date-stamped project evidence, not screenshots captured during a fresh Play Mode test.')
para('Team contribution split, exact calendar timeline, supervisor designation, exact school age/grade band, minimum hardware, approved certificate wording, and formal declaration text remain unspecified. The title page contains only the supplied student and supervisor details and the Project-I format information.')
h1('APPENDIX C: ACADEMIC APPROVAL STATUS')
para('The certificate and candidate declaration pages are included before the abstract, based on the supplied institutional templates and updated to CEUP301: Project-I, Semester 5. The supplied declaration template named CE363 Project-III, so its course reference has been adapted. All signature and approval lines are blank. Confirm the final wording and the guide designation with the department before signing or submission; these pages are not represented as approved or signed.')

# Header/footer and update fields
for s in doc.sections:
 s.header_distance=Inches(.45); s.footer_distance=Inches(.45)
 hp=s.header.paragraphs[0]; hp.alignment=WD_ALIGN_PARAGRAPH.LEFT; hp.paragraph_format.line_spacing=1
 rr=hp.add_run('CEUP301  |  PROJECT-I'); rr.font.name='Times New Roman'; rr.font.size=Pt(9); rr.bold=True
 fp=s.footer.paragraphs[0]; fp.alignment=WD_ALIGN_PARAGRAPH.RIGHT; fp.paragraph_format.line_spacing=1
 rr=fp.add_run('C.S.P.I.T.  •  COMPUTER ENGINEERING     '); rr.font.size=Pt(9); rr.font.name='Times New Roman'
 field(fp.add_run(),'PAGE')
settings=doc.settings._element
uf=OxmlElement('w:updateFields'); uf.set(qn('w:val'),'true'); settings.append(uf)
doc.core_properties.title='Educational Space Game — Project-I Report'
doc.core_properties.subject='Project-I, Semester 5, Computer Engineering'
doc.core_properties.author='Khush Vaghani and Shreejay Shiyani'
doc.core_properties.keywords='Unity, educational game, physics, visual programming, space'
doc.save(DOC)
print(DOC)
