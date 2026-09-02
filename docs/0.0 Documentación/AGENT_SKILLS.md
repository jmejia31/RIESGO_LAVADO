# Arquitectura Agent Skills — RIESGO_LAVADO

## 1. Objetivo

Establecer una capa operativa común para que ChatGPT, Codex/Codex CLI, Antigravity y futuros colaboradores trabajen con las mismas reglas del proyecto y reduzcan errores por pérdida de contexto, instrucciones divergentes o procedimientos distintos.

## 2. Fuente normativa

El formato adoptado es **Agent Skills**, especificado públicamente en:

- `https://github.com/agentskills/agentskills`
- `https://agentskills.io/specification`

La especificación define cada skill como una carpeta con `SKILL.md` obligatorio y permite recursos opcionales como `scripts/`, `references/` y `assets/`.

### Decisión del proyecto

RIESGO_LAVADO **no** incluye el repositorio `agentskills/agentskills` como submódulo ni copia su código. Se usa como estándar externo. Esto evita dependencia operativa, duplicación y drift innecesario.

El proyecto implementa un validador propio sin dependencias de terceros para las reglas esenciales utilizadas aquí. La librería oficial `skills-ref` puede servir como referencia adicional, pero su README declara que no está destinada a producción.

## 3. Jerarquía de autoridad

1. Instrucción expresa vigente de Javier Mejía.
2. `AGENTS.md`.
3. Estado y bitácora del repositorio.
4. Agent Skills aplicables.
5. Documentación técnica del módulo.
6. Convenciones generales de la herramienta/agente.

Una skill nunca autoriza modificar `main`, cerrar una fase funcionalmente ni ignorar un gate obligatorio.

## 4. Estructura

```text
.agents/
├── AGENTS.md
└── skills/
    ├── README.md
    ├── riesgo-lavado-continuidad/
    │   └── SKILL.md
    ├── riesgo-lavado-cambio-quirurgico/
    │   └── SKILL.md
    ├── riesgo-lavado-quality-gates/
    │   └── SKILL.md
    ├── riesgo-lavado-cierre-fase/
    │   └── SKILL.md
    └── ... skills especializadas ...
```

## 5. Modelo de activación

Agent Skills usa descubrimiento progresivo: el cliente conoce `name` + `description`, carga el `SKILL.md` completo cuando la tarea coincide y consulta recursos adicionales solo cuando son necesarios.

Por ello, las descripciones deben indicar claramente **qué hace la skill y cuándo usarla**.

Una tarea puede y debe activar varias skills si cruza dominios.

## 6. Catálogo inicial

### Transversales

- `riesgo-lavado-continuidad`
- `riesgo-lavado-cambio-quirurgico`
- `riesgo-lavado-quality-gates`
- `riesgo-lavado-cierre-fase`

### Especializadas

- `riesgo-lavado-frontend-angular`
- `riesgo-lavado-backend-aspnet`
- `riesgo-lavado-oracle-database`
- `riesgo-lavado-ui-ux-ihss`
- `riesgo-lavado-matrices-riesgo`
- `riesgo-lavado-testing-regresion`
- `riesgo-lavado-documentacion`
- `riesgo-lavado-github-ci`
- `riesgo-lavado-sonarcloud`
- `riesgo-lavado-pdf-excel`

## 7. Integridad

Validador local:

```bash
python tools/validate_agent_skills.py
```

CI:

```text
.github/workflows/agent-skills.yml
```

Se valida:

- existencia de `.agents/skills/`;
- `SKILL.md` por directorio;
- frontmatter delimitado;
- `name` obligatorio;
- `description` obligatoria;
- nombre <= 64 caracteres;
- nombre en minúsculas/números/guiones;
- ausencia de guiones consecutivos;
- coincidencia entre `name` y carpeta;
- límites de `description` y `compatibility`;
- política del proyecto de máximo 500 líneas por `SKILL.md`.

## 8. Regla para nuevos colaboradores

Un nuevo colaborador no necesita recibir un prompt maestro privado. Debe:

1. clonar/actualizar `desarrollo`;
2. leer `AGENTS.md`;
3. recuperar estado/bitácora;
4. dejar que su cliente descubra `.agents/skills/` o leer explícitamente las skills aplicables;
5. trabajar y publicar el handoff en `origin/desarrollo`.

Así, la continuidad depende del repositorio y no de la memoria de una conversación o de una máquina concreta.

## 9. Evolución

Crear una nueva skill únicamente cuando exista un workflow reutilizable y distinto. Evitar skills gigantes o duplicadas. Si una skill crece, mover detalle a `references/` y mantener `SKILL.md` conciso.

Los agentes específicos de una herramienta (`.codex/agents/`, equivalentes futuros de Antigravity, etc.) pueden añadirse como capa opcional, pero **no deben duplicar las reglas de dominio**: deben invocar o apoyarse en estas Agent Skills compartidas.
