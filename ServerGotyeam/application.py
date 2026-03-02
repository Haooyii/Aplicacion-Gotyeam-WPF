from datetime import datetime, timedelta
from bson import ObjectId
from flask import Flask, jsonify, request
from flask_jwt_extended import JWTManager, create_access_token, get_jwt, get_jwt_identity, jwt_required
from jsonschema import validate
from pymongo import MongoClient
import config
from flask_cors import CORS
import bcrypt
from seguridad import requiere_rol

client = MongoClient(config.MONGODB_CONNECTION_STRING)
db = client.GotyeamDB

application = Flask(__name__)
CORS(application)


application.config["JWT_SECRET_KEY"] = "super-secret"
application.config["JWT_ACCESS_TOKEN_EXPIRES"] = timedelta(hours=10)
jwt = JWTManager(application)

@application.route("/")
def hello_world():
    return "API Levantada", 200


#ENDPOINTS EMPLEADOS:

#login del empleado, se comprueba el correo y la contraseña y que esté activo, en caso contrario se devuelve un error
@application.route('/empleado/login', methods=['POST'])
def login_empleado():
    try:
        # login_data obtiene los datos del login del usuario en formato JSON
        login_data:dict = request.get_json()

        #schema es el formato obligatorio que debe tener el JSON del login del cliente, 
        # en este caso debe tener un correo y una contraseña, ambos de tipo string, 
        # el correo debe tener al menos 3 caracteres y no se permiten propiedades adicionales
        schema:dict = {
                "$schema": "http://json-schema.org",
                "type": "object",
                "properties": {
                    "correo": { "type": "string", "minLength": 3 },
                    "contrasenna": { "type": "string" }
                },
                "required": ["correo", "contrasenna"],
                "additionalProperties": False
            }
        
        #validate comprueba que el JSON del login del cliente cumple con el formate del schema
        validate(instance=login_data, schema=schema)

        #proyection es para definir que campos de la BD traerte a tu servidor, 1 significa que quieres 
        #ese campo y 0 que no. Se trae la contraseña para poder compararla con la contraseña que el cliente envió
        projection:dict = {
            '_id': 1,
            'nivel_rol': 1,
            'contrasenna': 1,
            'activo': 1
        }

        #busca el empleado por su correo y se trae al servidor los campos que definimos en projection
        empleado = db.empleados.find_one({"correo": login_data['correo']}, projection)


        if empleado and bcrypt.checkpw(login_data['contrasenna'].encode('utf-8'), empleado['contrasenna'].encode('utf-8')):
            #compruebo que el usuario no esté desactivado (esto es equivalente a despedido)
            if empleado['activo'] == False:
                return jsonify({"msg": "El empleado está desactivado"}), 401
            access_token = create_access_token(identity=str(empleado['_id']), additional_claims={'nivel_rol': empleado['nivel_rol']})
            return jsonify(access_token=access_token), 200
        return '', 401

    except Exception:
        return '', 400

#tú no puedes registrarte por tu cuenta, necesitas que un empleado con el rol adecuado te registre,en caso de que el empleado
#a registrar ya exista, se comprueba si está activo o no, si existe y está activo se devuelve un error, si existe y está inactivo
#se reactivo con los nuevos datos, si no existe se crea un nuevo empleado
@application.route('/empleado/registrar', methods=['POST'])
@requiere_rol([3,4])
def registrar_empleado():
    try:
        register_data:dict = request.get_json()
        schema:dict = {
                "$schema": "http://json-schema.org",
                "type": "object",
                "properties": {
                    "nombre": { "type": "string" },
                    "apellido1": { "type": "string" },
                    "apellido2": { "type": "string" },
                    "correo": { "type": "string"},
                    "contrasenna": { "type": "string" },
                    "nivel_rol": { "type": "integer", "minimum": 1, "maximum": 4 },
                    "foto_url": { "type": "string" }
                },
                "required": ["nombre", "apellido1", "correo", "contrasenna", "nivel_rol","foto_url"],
                "additionalProperties": False
            }
        
        validate(instance=register_data, schema=schema)
        
        claims = get_jwt()
        mi_rol = claims.get('nivel_rol',0)
        rol_nuevo = register_data['nivel_rol']

        if mi_rol == 3 and rol_nuevo not in [1,2]:
            return jsonify({"msg": "Un Nivel 3 solo puede crear empleados de Nivel 1 o 2"}), 403

        register_data['contrasenna'] = bcrypt.hashpw(register_data['contrasenna'].encode('utf-8'),bcrypt.gensalt()).decode('utf-8')

        empleado_encontrado = db.empleados.find_one({"correo": register_data['correo']})

        #compruebo que el empleado a registrar no exista y en caso de que exista, comprobar que esté desactivado
        if empleado_encontrado:
            if empleado_encontrado['activo']:
                return jsonify({"msg": "El correo ya está registrado"}), 400
            else: 
                db.empleados.update_one({"correo":register_data['correo']}, {"$set": {**register_data, 'activo': True}})
                return jsonify({"msg":"Empleado reactivado exitosamente"}),200
        else:
            register_data['activo'] = True
            db.empleados.insert_one(register_data)
            return jsonify({"msg":"Empleado registrado exitosamente"}),201
    
    except Exception as e:
        return jsonify({"msg":"Error en el registro","error": str(e)}), 500

#listar empleados se puede filtrar por estado (activo,inactivo o todos, por defecto es activo) y por texto (en los campos
#nombre, apellido1, apellido2 y correo)
@application.route('/empleado/listar', methods=['GET'])
@requiere_rol([3,4])
def listar_empleados():  
    try:
        estado = request.args.get('estado','activo').lower()
        buscar = request.args.get('buscar','')

        #para comprbar el estado del empleado, si es activo, inactivo o todos. Si no se especifica, por defecto se muestran los activos
        if estado == 'activo':
            query = {'activo':True}
        elif estado == 'inactivo':
            query = {'activo': False}
        elif estado == 'todos':
            query = {}

        #comprobar si se ha enviado un parámetro de búsqueda, si se ha enviado, se crea un filtro de texto para buscar en los campos nombre, 
        #apellido1, apellido2 y correo. Se utiliza regex para hacer una búsqueda parcial y se ignora mayúsculas y minúsculas. 
        #Si no se ha enviado un parámetro de búsqueda, se muestra la lista de empleados sin filtrar por texto.
        if buscar:
            filtro_texto = {"$regex":buscar,"$options":"i"}
            query["$or"] = [
                {"nombre":filtro_texto},
                {"apellido1":filtro_texto},
                {"apellido2":filtro_texto},
                {"correo":filtro_texto},
            ]

        lista_empleados = list(db.empleados.find(query,{'contrasenna':0}))
        #esto se hace para convertir el ObjectId de MongoDB a string, porque el ObjectId es
        #un tipo de dato especial de MongoDB que no se puede convertir a JSON directamente.
        for empleado in lista_empleados:
            empleado['_id'] = str(empleado['_id'])
        return jsonify(lista_empleados), 200

    except Exception as e:
        return jsonify({"msg":"Error al listar empleados","error": str(e)}), 500

#mostrar los detalles de un empleado en concreto
@application.route('/empleado/detalle/<string:id>', methods=['GET'])
@requiere_rol([3,4])
def detalle_empleado(id):
    try:
        empleado = db.empleados.find_one({"_id": ObjectId(id)}, {'contrasenna':0})
        if empleado:
            empleado['_id'] = str(empleado['_id'])
            return jsonify(empleado), 200
        else:
            return jsonify({"msg":"Empleado no encontrado"}), 404
    except Exception as e:
        return jsonify({"msg":"Error al obtener detalle del empleado","error": str(e)}), 500

#mostrar los detalles del propio empleado que ha iniciado sesión
@application.route('/empleado/mis_detalles', methods=['GET'])
@jwt_required()
def mis_detalles_empleado():
    try:
        mi_id = get_jwt_identity()

        empleado = db.empleados.find_one({"_id": ObjectId(mi_id)}, {'contrasenna':0})
        if empleado:
            empleado['_id'] = str(empleado['_id'])
            return jsonify(empleado), 200
        else:
            return jsonify({"msg":"Empleado no encontrado"}), 404
    except Exception as e:
        return jsonify({"msg":"Error al obtener tus detalles","error": str(e)}), 500

#editar empleado, en el caso de que sea mi propio perfil, no puede editar mi rol, en el caso de que sea un empleado nivel 3, solo 
#pueda cambiar el nivel de rol de otro empleado a 1 o 2, no se cambia el estado ya que eso está en otro endpoint
@application.route('/empleado/editar/<string:id_destino>',methods=['PATCH'])
@jwt_required()
def editar_empleado(id_destino):
    try:
        mi_id = get_jwt_identity()
        mi_rol = get_jwt().get('nivel_rol',0)
        edit_data:dict = request.get_json()
        empleado_destino = db.empleados.find_one({"_id": ObjectId(id_destino)}, {'contrasenna':0})

        schema:dict = {
                "$schema": "http://json-schema.org",
                "type": "object",
                "properties": {
                    "nombre": { "type": "string" },
                    "apellido1": { "type": "string" },
                    "apellido2": { "type": "string" },
                    "correo": { "type": "string"},
                    "contrasenna": { "type": "string" },
                    "nivel_rol": { "type": "integer", "minimum": 1, "maximum": 4 },
                    "foto_url": { "type": "string" }
                },
                "additionalProperties": False
            }
        
        validate(instance=edit_data, schema=schema)

        es_mi_perfil = (mi_id == id_destino)
        soy_admin = mi_rol in [3,4]

        #en caso de que no sea mi perfil y no sea admin, no tengo permiso para editar el perfil
        if not es_mi_perfil and not soy_admin:
            return jsonify({"msg":"No tienes permiso para editar este perfil"}), 401
        
        #comprobar que el empleado no pueda editar su propio nivel de rol y que un rol nivel 3 no pueda editar
        #el nivel de rol de otro empleado que no sea nivel 1 o 2 y que el nuevo rol de ese empleado no sea tampoco un rol que no sea nivel 1 o 2
        if 'nivel_rol' in edit_data:
            nuevo_rol = edit_data['nivel_rol']
            if es_mi_perfil:
                if nuevo_rol != empleado_destino.get('nivel_rol'):
                    return jsonify({"msg":"No puedes editar tu propio rol"}), 402
                else:
                    edit_data.pop('nivel_rol',None)
            
            elif mi_rol == 3:
                if empleado_destino['nivel_rol'] not in [1,2]:
                    return jsonify({"msg":"Un Nivel 3 solo puede cambiar el rol de otro de Nivel 1 o 2"}), 403
                elif nuevo_rol not in [1,2]:
                    return jsonify({"msg":"Un Nivel 3 solo puede asignar roles de Nivel 1 o 2"}), 405
        
        #comprobar que la contraseña no esté vacía, en caso de estarlo, eliminarlo del diccionario
        if edit_data.get('contrasenna') and edit_data['contrasenna'].strip() != '':
            edit_data['contrasenna'] = bcrypt.hashpw(edit_data['contrasenna'].encode('utf-8'),bcrypt.gensalt()).decode('utf-8')
        else:
            edit_data.pop('contrasenna', None)

        #comprobación adicional para evitar modificar el id y el estado del empleado en mongoDB
        edit_data.pop('_id', None)
        edit_data.pop('activo',None)
        result = db.empleados.update_one({"_id": ObjectId(id_destino)}, {"$set": edit_data})

        if result.matched_count == 1:
            return jsonify({"msg":"Empleado editado exitosamente"}), 200
        else:
            return jsonify({"msg":"No se realizaron cambios"}), 404

    except Exception as e:
        return jsonify({"msg":"Error al editar empleado","error":str(e)}), 500

#editar el estado de un empleado para ponerlo al contrario del que esté (sirve por si quiero poner un botón que cambie rápidamente el estado)
#compruebo que no pueda editar mi propio estado y que un empleado nivel 3 solo pueda editar el estado de un empleado nivel 1 o 2 
@application.route('/empleado/estado/<string:id_destino>', methods=['PATCH'])
@requiere_rol([3,4])
def editar_estado_empleado(id_destino):
    try:
        mi_id = get_jwt_identity()
        mi_rol = get_jwt().get('nivel_rol',0)
        empleado_destino = db.empleados.find_one({"_id": ObjectId(id_destino)},{'contrasenna':0})

        es_mi_perfil = (mi_id == id_destino)
        soy_admin = mi_rol in [3,4]

        #comprobar que no pueda editar su propio estado
        if es_mi_perfil:
            return jsonify({"msg":"No puedes editar tu propio estado"}), 403
        
        #comprobar que si no es tu perfil y no eres admin, no tienes permiso para editar el estado del perfil
        if not es_mi_perfil and not soy_admin:
            return jsonify({"msg":"No tienes permiso para editar el estado de este perfil"}), 403
        
        #asegurar que un empleado con nivel 3 solo puede editar empleados de nivel 1 o 2
        if mi_rol == 3 and empleado_destino['nivel_rol'] not in [1,2]:
            return jsonify({"msg":"Un Nivel 3 solo puede editar el estado de otro Nivel 1 o 2"}), 403
        
        nuevo_estado = not empleado_destino.get('activo',True)

        db.empleados.update_one({"_id": ObjectId(id_destino)}, {"$set": {'activo': nuevo_estado}})

        if nuevo_estado:
            return jsonify({"msg":"Empleado reactivado exitosamente"}), 200
        else:
            return jsonify({"msg":"Empleado desactivado exitosamente"}), 200

    except Exception as e:
        return jsonify({"msg":"Error al editar estado del empleado","error":str(e)}), 500

#ENDPOINTS JUEGOS:

#compruebo si el juego a añadir ya existe por su id de la api de Rawg o por su título en caso de que no tenga id de la api de Rawg.
#si existe el juego, se compruebo si está activo o no, si está inactivo se reactiva con los nuevos datos, en caso contrario devuelve error
@application.route('/juego/annadir', methods=['POST'])
@requiere_rol([1,4])
def annadir_juego():
    try:
        annadir_data:dict = request.get_json()
        schema:dict = {
                "$schema": "http://json-schema.org",
                "type": "object",
                "properties": {
                    "apiRawg_id": {"type": ["integer","null"]},
                    "titulo": { "type": "string" },
                    "portada_url": { "type": "string" },
                    "descripcion": { "type": "string" },
                    "generos": { "type": "array","maxItems": 3, "items": { "type": "string" }},
                    "fecha_lanzamiento": {"type": "string", "pattern": "^\\d{4}-\\d{2}-\\d{2}$"},
                    "activo":{"type":"boolean"}
                },
                "required": ["titulo", "portada_url", "descripcion", "generos", "fecha_lanzamiento","activo"],
                "additionalProperties": False
            }
        
        validate(instance=annadir_data, schema=schema)

        #si el juego tiene un apiRawg_id, se busca por ese campo, si no, se busca por título, ignorando mayúsculas y minúsculas, para evitar juegos duplicados
        if annadir_data.get('apiRawg_id'):
            query = {"apiRawg_id": annadir_data['apiRawg_id']}
        else:
            query = {"titulo": {"$regex": f"^{annadir_data['titulo']}$", "$options": "i"}}

        juego_existe = db.juegos.find_one(query)
        empleado_actual = get_jwt_identity()
        fecha_actual = datetime.now().strftime("%Y-%m-%d")

        #si ya existe el juego, se comprueba si está activo o no
        if juego_existe:
            if juego_existe.get('activo'):
                return jsonify({"msg":"Ya existe un juego con este título o ID de RAWG en el catálogo"}), 400
            else:
                annadir_data.update({
                        "modificado_por": ObjectId(empleado_actual), 
                        "fecha_modificacion": fecha_actual,
                    })
                db.juegos.update_one({"_id": juego_existe['_id']}, {"$set": {**annadir_data, 'activo': True}})
                return jsonify({"msg":"Juego reactivado exitosamente"}), 200

        annadir_data.update({
            "registrado_por": ObjectId(empleado_actual),
            "modificado_por": ObjectId(empleado_actual), 
            "fecha_registro": fecha_actual, 
            "fecha_modificacion": fecha_actual,
            "valoracion_promedia": 0.0,
            "resennas_totales": 0
        })

        result = db.juegos.insert_one(annadir_data)

        if result.inserted_id:
            return jsonify({"msg":"Juego añadido exitosamente"}), 201

    except Exception as e: 
        return jsonify({"msg":"Error al añadir juego","error":str(e)}), 500

#comprueba si el juego a editar tiene apiRawg_id, si lo tiene, no puede modificar el título para no tener varios juegos con el mismo nombre, si 
#no tiene (juego añadido manualmente por un empleado) busca si ya existe un juego con el mismo título para evitar duplicados (busca independientemente de si
#tiene el apiRawg_id o no)
@application.route('/juego/editar/<string:id>', methods=['PATCH'])
@requiere_rol([1,4])
def editar_juego(id):
    try:
        edit_data:dict = request.get_json()

        schema:dict = {
                "$schema": "http://json-schema.org",
                "type": "object",
                "properties": {
                    "descripcion": { "type": "string" },
                    "generos": { "type": "array","maxItems": 3, "items": { "type": "string" }},
                    "fecha_lanzamiento": {"type": "string", "pattern": "^\\d{4}-\\d{2}-\\d{2}$"}
                },
                "additionalProperties": False
            }
        
        validate(instance=edit_data, schema=schema)

        #sirve para que no se pueda modificar ciertos campos
        campos_protegidos = ['registrado_por','fecha_registro','valoracion_promedia','resennas_totales',"_id","apiRawg_id"]

        juego_db = db.juegos.find_one({"_id": ObjectId(id)})

        if not juego_db:
            return jsonify({"msg":"Juego no encontrado"}), 404
        
        #comprobación para que si el juego ha sido añadido de la API de Rawg, no se pueda modificar su título,así no hay varios juegos con el mismo nombre
        if juego_db.get('apiRawg_id'):
            if edit_data.get('titulo') or edit_data('portada_url'):
                return jsonify({"msg":"No puedes editar el título o la portada de un juego importado de la API RAWG"}), 403

        empleado_actual = get_jwt_identity()
        fecha_actual = datetime.now().strftime("%Y-%m-%d")

        for campo in campos_protegidos:
            edit_data.pop(campo,None)

        edit_data.update({
            "modificado_por": ObjectId(empleado_actual),
            "fecha_modificacion": fecha_actual
        })    

        result = db.juegos.update_one({"_id": ObjectId(id)}, {"$set": edit_data})

        if result.matched_count == 1:
            return jsonify({"msg":"Juego modificado exitosamente"}), 200
        else:
            return jsonify({"msg":"No se realizó ningún cambio"}), 404
    
    except Exception as e:
        return jsonify({"msg":"Error al modificar juego","error":str(e)}), 501

#cambiar el estado de un juego para ponerlo al contrario del que esté (sirve por si quiero poner un botón que cambie rápidamente el estado)
@application.route('/juego/estado/<string:id_destino>', methods=['PATCH'])
@requiere_rol([1,4])
def editar_estado_juego(id_destino):
    try:
        empleado_actual = get_jwt_identity()
        juego_destino = db.juegos.find_one({"_id": ObjectId(id_destino)})

        estado_data:dict = {
            "modificado_por":ObjectId(empleado_actual),
            "fecha_modificacion": datetime.now().strftime("%Y-%m-%d"),
            "activo": not juego_destino.get('activo',True)
        }

        db.juegos.update_one({"_id": ObjectId(id_destino)}, {"$set": estado_data})

        if estado_data['activo']:
            return jsonify({"msg":"Juego reactivado exitosamente"}), 200
        else:
            return jsonify({"msg":"Juego desactivado exitosamente"}), 200

    except Exception as e:
        return jsonify({"msg":"Error al editar estado del juego","error":str(e)}), 500

#listo los juegos dependiendo de si es un usuario normal o un empleado con el rol necesario(1,2,4. el nivel 2 porque quiero que el empleado pueda
#ver reseñas por juegos), el usuario normal solo puede ver los juegos activos mientras que el empleado puede listar por activo, inactivo o todos 
#(por defecto está activo) y filtrar por título o género (sirve como buscador de juegos tmbién)
@application.route('/juego/listar', methods=['GET'])
@jwt_required()
def listar_juegos():
    try:
        mi_rol = get_jwt().get('nivel_rol',0)

        estado = request.args.get('estado','activo').lower()
        buscar = request.args.get('buscar','')
        genero = request.args.get('genero','')
        query = {}
        projection = {
            '_id': 1,
            'titulo': 1,
            'portada_url': 1,
            'generos': 1,
            'fecha_lanzamiento': 1,
            'valoracion_promedia': 1,
            'resennas_totales': 1
        }


        if mi_rol == 0:
            query['activo'] = True
        elif mi_rol in [1,2,4]:
            if estado == 'activo':
                query['activo'] = True
            elif estado == 'inactivo':
                query['activo'] = False

        if buscar:
            query['titulo'] = {"$regex":buscar, "$options":"i"}
        
        if genero:
            query['generos'] = genero

        #te busca el juego por tu consulta, te salta juegos para que no volver a mostrar los mismos juegos en la siguiente page
        #y te limita el numero de juegos que te muestra por page
        lista_juegos = list(db.juegos.find(query,projection))

        for juego in lista_juegos:
            juego['_id'] = str(juego['_id'])
            
        return jsonify(lista_juegos), 200


    except Exception as e:
        return jsonify({"msg":"Error al listar juegos","error":str(e)}), 500

#mostrar el detalle de un juego dependiendo de si es un usuario normal o un empleado con el rol necesario(1,2,4, el 2 para que el empleado compruebe que es el juego
#del que quiere ver las reseñas), el usuario normal solo puede ver los juegos activos 
@application.route('/juego/detalle/<string:id>', methods=['GET'])
@jwt_required()
def detalle_juego(id):
    try:
        token = get_jwt()
        if token:
            mi_rol = token.get('nivel_rol',0)
            if len(id) == 24:
                filtro_base = {"_id": ObjectId(id)}
            else:
                filtro_base = {"apiRawg_id": int(id)}
                
            if mi_rol == 0:
                filtro_final = {**filtro_base, "activo": True}
                juego = db.juegos.find_one(filtro_final)
            elif mi_rol in [1,2,4]:
                juego = db.juegos.find_one(filtro_base)
            else:
                return jsonify({"msg":"No tienes permiso para ver el detalle de este juego"}), 403

        if juego:
            juego['_id'] = str(juego['_id'])
            juego['registrado_por'] = str(juego['registrado_por'])
            juego['modificado_por'] = str(juego['modificado_por'])
            return jsonify(juego), 200
        else:
            return jsonify({"msg":"Juego no encontrado"}), 404
    except Exception as e:
        return jsonify({"msg":"Error al obtener detalle del juego","error":str(e)}), 500
    
#ENDPOINTS USUARIOS:

#login del usuario por correo y se comprueba que esté activo, en caso contrario devuelve error
@application.route('/usuario/login', methods=['POST'])
def login_usuario():
    try:
        # login_data obtiene los datos del login del usuario en formato JSON
        login_data:dict = request.get_json()

        #schema es el formato obligatorio que debe tener el JSON del login del cliente, 
        # en este caso debe tener un correo y una contraseña, ambos de tipo string, 
        # el correo debe tener al menos 3 caracteres y no se permiten propiedades adicionales
        schema:dict = {
                "$schema": "http://json-schema.org",
                "type": "object",
                "properties": {
                    "correo": { "type": "string", "minLength": 3 },
                    "contrasenna": { "type": "string" }
                },
                "required": ["correo", "contrasenna"],
                "additionalProperties": False
            }
        
        #validate comprueba que el JSON del login del cliente cumple con el formate del schema
        validate(instance=login_data, schema=schema)

        #proyection es para definir que campos de la BD traerte a tu servidor, 1 significa que quieres 
        #ese campo y 0 que no. Se trae la contraseña para poder compararla con la contraseña que el cliente envió
        projection:dict = {
            '_id': 1,
            'username': 1,
            'contrasenna': 1,
            'activo': 1
        }

        #busca el usuario por su correo y se trae al servidor los campos que definimos en projection
        usuario = db.usuarios.find_one({"correo": login_data['correo']}, projection)


        if usuario and bcrypt.checkpw(login_data['contrasenna'].encode('utf-8'), usuario['contrasenna'].encode('utf-8')):
            #compruebo que el usuario no esté desactivado (esto es equivalente a despedido)
            if usuario['activo'] == False:
                return jsonify({"msg": "El usuario está desactivado"}), 401
            access_token = create_access_token(identity=str(usuario['_id']), additional_claims={'username': usuario['username']})
            return jsonify(access_token=access_token), 200
        return '', 401

    except Exception as e:
        return jsonify({"msg":"Error al iniciar sesión","error":str(e)}), 500

#registrar usuario, si existe ya pero está inactivo, se reactivo con los nuevos datos
@application.route('/usuario/registrar', methods=['POST'])
@jwt_required(optional=True)
def registrar_usuario():
    try:
        register_data:dict = request.get_json()
        schema:dict = {
                "$schema": "http://json-schema.org",
                "type": "object",
                "properties": {
                    "nombre": { "type": "string" },
                    "apellido1": { "type": "string" },
                    "apellido2": { "type": "string" },
                    "correo": { "type": "string"},
                    "contrasenna": { "type": "string" },
                    "username": { "type": "string", "minLength": 3 },
                    "foto_url": { "type": "string"}
                },
                "required": ["nombre", "apellido1", "correo", "contrasenna", "username"],
                "additionalProperties": False
            }
        
        validate(instance=register_data, schema=schema)
        
        #encriptar contraseña
        token = get_jwt()
        register_data['contrasenna'] = bcrypt.hashpw(register_data['contrasenna'].encode('utf-8'),bcrypt.gensalt()).decode('utf-8')
        
        #el empleado al poder registrar usuarios, se comprueba el token para ver su rol y si tiene el nivel necesario
        if token:
            mi_rol = token.get('nivel_rol',0)
            if mi_rol not in [2,4]:
                return jsonify({"msg": "Los empleados no pueden registrar usuarios"}), 403
        
        #en caso de que el usuario ya exista, se comprueba si está activo o no, si existe y está inactivo, se reactiva con nuevos datos, si no existe se crea un nuevo usuario
        usuario_encontrado = db.usuarios.find_one({"correo": register_data['correo']})
        if usuario_encontrado:
            if usuario_encontrado['activo']:
                return jsonify({"msg": "El correo ya está registrado"}), 400
            else: 
                db.usuarios.update_one({"correo":register_data['correo']}, {"$set": {**register_data, 'activo': True}})
                return jsonify({"msg":"Usuario reactivado exitosamente"}),200
        else :
            register_data.update({
                "activo": True,
                "favoritos": [],
                "tops": [None, None, None, None],
                "fecha_registro": datetime.now().strftime("%Y-%m-%d")
            })
            db.usuarios.insert_one(register_data)
            return jsonify({"msg":"Usuario registrado exitosamente"}),201


    except Exception as e:
        return jsonify({"msg":"Error al registrar usuario","error":str(e)}), 500

#se comprueba si quien está editando el perfil es el propio usuario o un empleado con el rol adecuado
@application.route('/usuario/editar/<string:id_cliente>', methods=['PATCH'])
@jwt_required()
def editar_usuario(id_cliente):
    try:
        #se comprueba si quien está editando los datos es el usuario mismo o un empleado con el rol adecuado
        token = get_jwt()
        mi_rol = token.get('nivel_rol',0)
        if not mi_rol == 0 and not mi_rol in [2,4]:
            return jsonify({"msg":"No tienes permiso para actualizar el perfil"}), 403
        else:
            editar_data:dict = request.get_json()
            schema:dict = {
                "$schema": "http://json-schema.org",
                "type": "object",
                "properties": {
                    "nombre": { "type": "string" },
                    "apellido1": { "type": "string" },
                    "apellido2": { "type": "string" },
                    "correo": { "type": "string"},
                    "contrasenna": { "type": "string" },
                    "username": { "type": "string", "minLength": 3 }
                },
                "additionalProperties": False
            }
            validate(instance=editar_data, schema=schema)
            if 'contrasenna' in editar_data and editar_data['contrasenna'].strip():
                editar_data['contrasenna'] = bcrypt.hashpw(editar_data['contrasenna'].encode('utf-8'),bcrypt.gensalt()).decode('utf-8')
            else:
                editar_data.pop('contrasenna', None)
            
            if mi_rol == 0:
                db.usuarios.update_one({"_id": ObjectId(token['sub'])}, {"$set": editar_data})
            elif mi_rol in [2,4]:
                db.usuarios.update_one({"_id": ObjectId(id_cliente)}, {"$set": editar_data})
            return jsonify({"msg":"Usuario actualizado exitosamente"}),200
    except Exception as e:
        return jsonify({"msg":"Error al actualizar usuario","error":str(e)}), 500

#cambiar el estado de un usuario para ponerlo al contrario del que esté (sirve por si quiero poner un botón que cambie rápidamente el estado)
#se comprueba si quien intenta editar los datos es el propio usuario o un empleado con el rol adecuado
@application.route('/usuario/estado/<string:id>', methods=['PATCH'])
@jwt_required()
def editar_estado_usuario(id):
    try:
        #verificar que quien intenta editar los datos sea el propio usuario o un empleado con el rol adecuado
        mi_rol = get_jwt().get('nivel_rol',0)
        if not mi_rol == 0 and not mi_rol in [2,4]:
            return jsonify({"msg":"No tienes permiso para cambiar el estado de otro usuario"}), 403
        else:
            usuario = db.usuarios.find_one({"_id": ObjectId(id)})
            nuevo_estado = not usuario.get('activo',True)
            db.usuarios.update_one({"_id": ObjectId(id)}, {"$set": {'activo': nuevo_estado}})
            if nuevo_estado:
                lista_resennas = list(db.resennas.find({"id_usuario": ObjectId(id)}))
                for resenna in lista_resennas:
                    db.resennas.update_one({"_id": resenna['_id']}, {"$set": {'visible': nuevo_estado}})
                return jsonify({"msg":"Usuario reactivado exitosamente"}), 200
            else:
                lista_resennas = list(db.resennas.find({"id_usuario": ObjectId(id)}))
                for resenna in lista_resennas:
                    db.resennas.update_one({"_id": resenna['_id']}, {"$set": {'visible': nuevo_estado}})
                return jsonify({"msg":"Usuario desactivado exitosamente"}), 200
    except Exception as e:
        return jsonify({"msg":"Error al cambiar estado del usuario","error":str(e)}), 500

#para ver los detalles de un usuario, se comprueba si quien intenta ver los detalles es el propio usuario o un empleado con el rol adecuado
@application.route('/usuario/detalles/<string:id>', methods=['GET'])
@jwt_required()
def detalles_usuario(id):   
    try:
        mi_rol = get_jwt().get('nivel_rol',0)

        if mi_rol not in [2,4]:
            return jsonify({"msg":"No tienes permiso para ver el detalle de este usuario"}), 403

        usuario = db.usuarios.find_one({"_id": ObjectId(id)}, {'contrasenna':0})
        if not usuario:
            return jsonify({"msg":"Usuario no encontrado"}), 404
        return jsonify(usuario), 200
    except Exception as e:
        return jsonify({"msg":"Error al obtener detalles del usuario","error":str(e)}), 500

#para mostrar los datos de mi perfil (username, fecha registro y tops), para que el usuario pueda cambiar sus datos personales, se usa el endpoint de detalles
@application.route('/usuario/perfil', methods=['GET'])
@jwt_required()
def mi_perfil():
    try:
        mi_id = get_jwt_identity()
        usuario = db.usuarios.find_one({"_id": ObjectId(mi_id)}, {"activo":True,'contrasenna':0})
        if not usuario:
            return jsonify({"msg":"Usuario no encontrado"}), 404
        if usuario.get('activo') == False:
            return jsonify({"msg":"El usuario está desactivado"}), 403
        
        pipeline = [
            {
                "$match": {"_id": ObjectId(mi_id)}
            },
            {
                "$lookup": {
                    "from": "juegos",
                    "localField": "tops",
                    "foreignField": "_id",
                    "as": "info_juegos_tops"
                }
            },

            {
                "$project": {
                    "_id": 1,
                    "username": 1,
                    "fecha_registro": 1,
                    "tops": 1,
                    "info_juegos_tops": {"_id": 1, "titulo": 1, "portada_url": 1}   
                }
                }
        ]

        result = list(db.usuarios.aggregate(pipeline))
        if not result:
            return jsonify({"msg":"Usuario no encontrado"}), 404

        usuario = result[0]
        usuario['_id'] = str(usuario['_id'])
        usuario['tops'] = [str(top) if top else None for top in usuario.get('tops',[])] 

        for juego in usuario.get('info_juegos_tops',[]):
            juego['_id'] = str(juego    ['_id'])

    except Exception as e:
        return jsonify({"msg":"Error al obtener tus detalles","error":str(e)}), 500

#para listar los usuarios, tmb sirve como buscador de usuarios, puedes listar los usuarios por estado (activo, inactivo o todos) y por texto (nombre completo, username o correo)
@application.route('/usuario/listar', methods=['GET'])
@requiere_rol([2,4])
def listar_usuarios():
    try:
        estado = request.args.get('estado','activo').lower()
        buscar = request.args.get('buscar','')

        if estado == 'activo':
            query = {'activo':True}
        elif estado == 'inactivo':
            query = {'activo': False}
        elif estado == 'todos':
            query = {}

        projection:dict = {
            'favoritos': 0,
            'tops': 0,
            'contrasenna':0

        }

        if buscar:
            filtro_texto = {"$regex":buscar,"$options":"i"}
            query["$or"] = [
                {"nombre":filtro_texto},
                {"apellido1":filtro_texto},
                {"apellido2":filtro_texto},
                {"correo":filtro_texto},
                {"username": filtro_texto}
            ]

        lista_usuarios = list(db.usuarios.find(query,projection))
        for usuario in lista_usuarios:
            usuario['_id'] = str(usuario['_id'])
        return jsonify(lista_usuarios), 200

    except Exception as e:
        return jsonify({"msg":"Error al listar usuarios","error":str(e)}), 500

#añadir un juego como favorito
@application.route('/usuario/favorito/annadir/<string:id_juego>', methods=['POST'])
@jwt_required()
def annadir_favorito(id_juego):
    try:
        mi_id = get_jwt_identity()

        juego_encontrado = db.juegos.find_one({"_id": ObjectId(id_juego), "activo": True})
        if not juego_encontrado:
            return jsonify({"msg":"Juego no encontrado o inactivo"}), 404
        #$addToSet es para añadir el juego a favoritos sin que se duplique en caso de que ya esté añadido, si ya está añadido, no hace nada
        db.usuarios.update_one({"_id": ObjectId(mi_id)}, {"$addToSet": {"favoritos": ObjectId(id_juego)}})
        return jsonify({"msg":"Juego añadido a favoritos exitosamente"}), 200

    except Exception as e:
        return jsonify({"msg":"Error al añadir juego a favoritos","error":str(e)}), 500

#eliminar un juego de favoritos
@application.route('/usuario/favorito/eliminar/<string:id_juego>', methods=['PATCH'])
@jwt_required()
def eliminar_favorito(id_juego):
    try:
        mi_id = get_jwt_identity()

        juego_encontrado = db.juegos.find_one({"_id": ObjectId(id_juego), "activo": True})
        if not juego_encontrado:
            return jsonify({"msg":"Juego no encontrado o inactivo"}), 404
        #$pull es para eliminar el juego de favoritos, si el juego no está en favoritos, no hace nada
        db.usuarios.update_one({"_id": ObjectId(mi_id)}, {"$pull": {"favoritos": ObjectId(id_juego)}})
        return jsonify({"msg":"Juego eliminado de favoritos exitosamente"}), 200

    except Exception as e:
        return jsonify({"msg":"Error al eliminar juego de favoritos","error":str(e)}), 500

#listar juegos favoritos de un usuario, solo se muestran los juegos que estén activos
@application.route('/usuario/favorito/listar', methods=['GET'])
@jwt_required()
def listar_favoritos():
    try:
        mi_id = get_jwt_identity()

        usuario = db.usuarios.find_one({"_id": ObjectId(mi_id)}, {"favoritos": 1})
        if not usuario:
            return jsonify({"msg":"Usuario no encontrado"}), 404

        ids_favoritos = usuario.get('favoritos', [])
        favoritos_juegos = list(db.juegos.find({"_id": {"$in": ids_favoritos}, "activo": True}, {"titulo": 1, "portada_url": 1}))
        
        for juego in favoritos_juegos:
            juego['_id'] = str(juego['_id'])

        return jsonify(favoritos_juegos), 200

    except Exception as e:
        return jsonify({"msg":"Error al listar juegos favoritos","error":str(e)}), 500

#añadir top
@application.route('/usuario/top/asignar/<int:posicion>/<string:id_juego>', methods=['POST'])
@jwt_required()
def asignar_top(posicion, id_juego):
    try:
        mi_id = get_jwt_identity()

        if posicion < 0 or posicion > 3:
            return jsonify({"msg":"La posición del top debe ser entre 1 y 4"}), 400

        juego_encontrado = db.juegos.find_one({"_id": ObjectId(id_juego), "activo": True})
        if not juego_encontrado:
            return jsonify({"msg":"Juego no encontrado o inactivo"}), 404

        #$set es para asignar el juego a la posición del top, por ejemplo, si la posición es 1, se asigna el juego a top1, si es 2, se asigna a top2, etc
        db.usuarios.update_one({"_id": ObjectId(mi_id)}, {"$set": {f'tops.{posicion}': ObjectId(id_juego)}})
        return jsonify({"msg":f"Juego asignado a top {posicion + 1} exitosamente"}), 200

    except Exception as e:
        return jsonify({"msg":"Error al asignar juego al top","error":str(e)}), 500

@application.route('/usuario/top/listar', methods=['GET'])
@jwt_required()
def listar_top():
    try:
        mi_id = get_jwt_identity()

        # Seguimos el patrón de agregación de 'listar_mis_resennas' 
        pipeline = [
            # 1. Buscamos al usuario actual 
            {"$match": {"_id": ObjectId(mi_id)}},
            
            # 2. Hacemos el cruce (lookup) con la colección de juegos usando el array 'tops' 
            {
                "$lookup": {
                    "from": "juegos",
                    "localField": "tops",
                    "foreignField": "_id",
                    "as": "info_juegos_tops"
                }
            },
            
            # 3. Proyectamos solo lo que necesitamos para el cliente Android 
            {
                "$project": {
                    "_id": 0,
                    "tops": 1, # Mantenemos los IDs originales para saber el orden y los huecos (None)
                    "info_juegos_tops": {"_id": 1, "titulo": 1, "portada_url": 1}
                }
            }
        ]

        resultado = list(db.usuarios.aggregate(pipeline))
        
        if not resultado:
            return jsonify({"msg": "Usuario no encontrado"}), 404

        datos = resultado[0]
        # Reconstruimos la lista de 4 posiciones para que Android sepa dónde hay un 'None' 
        lista_final = []
        for id_juego in datos.get('tops', []):
            if id_juego is None:
                lista_final.append(None)
            else:
                # Buscamos el detalle del juego dentro de lo que trajo el lookup
                juego = next((j for j in datos['info_juegos_tops'] if j['_id'] == id_juego), None)
                if juego:
                    juego['_id'] = str(juego['_id'])
                lista_final.append(juego)

        return jsonify(lista_final), 200

    except Exception as e:
        return jsonify({"msg": "Error al listar los tops", "error": str(e)}), 500

#eliminar top
@application.route('/usuario/top/eliminar/<int:posicion>', methods=['PATCH'])
@jwt_required()
def eliminar_top(posicion):
    try:
        mi_id = get_jwt_identity()

        if posicion < 0 or posicion > 3:
            return jsonify({"msg":"La posición del top debe ser entre 1 y 4"}), 400

        #$set es para eliminar el juego de la posición del top, por ejemplo, si la posición es 1, se elimina el juego de top1, si es 2, se elimina de top2, etc
        db.usuarios.update_one({"_id": ObjectId(mi_id)}, {"$set": {f'tops.{posicion}': None}})
        return jsonify({"msg":f"Juego eliminado de top {posicion + 1} exitosamente"}), 200

    except Exception as e:
        return jsonify({"msg":"Error al eliminar juego del top","error":str(e)}), 500

#ENDPOINTS RESEÑAS:

#creamos la reseña para un juego activo y recalculamos la valoración promedio y el total de reseñas del juego
@application.route('/resenna/crear/<string:id_juego>', methods=['POST'])
@jwt_required()
def crear_resenna(id_juego):
    try:
        register_data = request.get_json()
        schema = {
            "$schema": "http://json-schema.org",
            "type": "object",
            "properties": {
                "comentario": {"type": "string"},
                "puntuacion": {"type": "integer", "minimum": 1, "maximum": 5},
                "recomendado": {"type": "boolean"}
            },
            "required": [ "comentario", "puntuacion", "recomendado"],
            "additionalProperties": False
        }

        validate(instance=register_data, schema=schema)

        juego_encontrado = db.juegos.find_one({"_id": ObjectId(id_juego), "activo": True})

        if juego_encontrado:
            register_data.update({
                "id_usuario": ObjectId(get_jwt_identity()),
                "id_juego": ObjectId(id_juego),
                "fecha_registro": datetime.now().strftime("%Y-%m-%d"),
                "fecha_modificacion": datetime.now().strftime("%Y-%m-%d"),
                "visible": True
            })
            db.resennas.insert_one(register_data)
            #devuelve una lista con todas las reseñas de un juego activo
            resennas_juego = list(db.resennas.find({"id_juego": ObjectId(id_juego), "visible": True}))
            #calculamos la longitud de la lista
            total_resennas = len(resennas_juego)
            #calculamos la valoración promedio sumando la puntuación de cada reseña y dividiéndola por el total de reseñas, si el total de reseñas es mayor que 0, si no, la valoración promedio es 0
            valoracion_promedio = sum(resenna['puntuacion'] for resenna in resennas_juego) / total_resennas if total_resennas > 0 else 0
            #actualizamos el juego con la nueva valoración promedio y el nuevo total de reseñas
            db.juegos.update_one({"_id": ObjectId(id_juego)}, {"$set": {"valoracion_promedia": valoracion_promedio, "resennas_totales": total_resennas}})
            return jsonify({"msg":"Reseña creada exitosamente"}), 201
        else:
            return jsonify({"msg":"Juego no encontrado o inactivo"}), 404

        
    except Exception as e:
        return jsonify({"msg":"Error al crear la reseña","error":str(e)}), 500

#para editar una reseña, solo el usuario que la creó puede editarla, se recalcula la valoración promedio
@application.route('/resenna/editar/<string:id_resenna>', methods=['PATCH'])
@jwt_required()
def editar_resenna(id_resenna): 
    try:
        edit_data = request.get_json()
        schema = {
            "$schema": "http://json-schema.org",
            "type": "object",
            "properties": {
                "comentario": {"type": "string"},
                "puntuacion": {"type": "integer", "minimum": 1, "maximum": 5},
                "recomendado": {"type": "boolean"}
            },
            "additionalProperties": False
        }
        validate(instance=edit_data, schema=schema)

        resenna_encontrada = db.resennas.find_one({"_id": ObjectId(id_resenna), "visible": True})

        if not resenna_encontrada:
            return jsonify({"msg":"Reseña no encontrada o no visible"}), 404
        
        if str(resenna_encontrada['id_usuario']) != get_jwt_identity():
            return jsonify({"msg":"No tienes permiso para editar esta reseña"}), 403
        
        edit_data.update({
            "fecha_modificacion":datetime.now().strftime("%Y-%m-%d")
        })

        db.resennas.update_one({"_id": ObjectId(id_resenna)}, {"$set": edit_data})

        resennas_juego = list(db.resennas.find({"id_juego": ObjectId(resenna_encontrada['id_juego']), "visible": True}))
        total_resennas = len(resennas_juego)
        valoracion_promedio = sum(resenna['puntuacion'] for resenna in resennas_juego) / total_resennas if total_resennas > 0 else 0
        db.juegos.update_one({"_id": ObjectId(resenna_encontrada['id_juego'])}, {"$set": {"valoracion_promedia": valoracion_promedio}})

        return jsonify({"msg":"Reseña editada exitosamente"}), 200
    except Exception as e:
        return jsonify({"msg":"Error al editar la reseña","error":str(e)}), 500

#eliminar una reseña, solo lo puede hacer el propio usuario o un empleado con el nivel de rol adecuado, se recalcula la valoración promedio y el total de reseñas del juego
@application.route('/resenna/eliminar/<string:id_resenna>', methods=['DELETE'])
@jwt_required()
def eliminar_resenna(id_resenna):
    try:
        mi_identidad = get_jwt_identity()
        mi_rol = get_jwt().get("nivel_rol",0)
        resenna_encontrada = db.resennas.find_one({"_id":ObjectId(id_resenna)})

        if mi_rol not in [0,2,4]:
            return jsonify({"msg":"No tienes permiso para eliminar esta reseña"}), 403
        elif mi_rol == 0 and str(resenna_encontrada['id_usuario']) != mi_identidad and resenna_encontrada['visible']:
            return jsonify({"msg":"No tienes permiso para eliminar esta reseña"}), 403
        else:
            db.resennas.delete_one({"_id": ObjectId(id_resenna)})
            resennas_juego = list(db.resennas.find({"id_juego": ObjectId(resenna_encontrada['id_juego'])}))
            total_resennas = len(resennas_juego)
            valoracion_promedio = (sum(resenna['puntuacion'] for resenna in resennas_juego)) / total_resennas if total_resennas > 0 else 0
            db.juegos.update_one({"_id": ObjectId(resenna_encontrada['id_juego'])}, {"$set": {"valoracion_promedia": valoracion_promedio, "resennas_totales": total_resennas}})
            return jsonify({"msg":"Reseña eliminada exitosamente"}), 200

    except Exception as e:
        return jsonify({"msg":"Error al eliminar la reseña","error":str(e)}), 500

#listar las reseñas de un juego en concreto, si lo hace el usuario normal, solo lista los juegos activos, mientras que si lo hace un empleado con el rol adecuado, puede
#listar por estado (activo, inactivo o todos), te muestra el username y la foto de perfil en el usuario
@application.route('/resenna/listar/<string:id_juego>', methods=['GET'])
@jwt_required()
def listar_resennas_juegos(id_juego):
    try:
        mi_rol = get_jwt().get("nivel_rol",0)

        juego_encontrado = db.juegos.find_one({"_id": ObjectId(id_juego)})

        if not juego_encontrado:
            return jsonify({"msg":"Juego no encontrado"}), 404
        
        filtro = {"id_juego": ObjectId(id_juego)}

        if mi_rol not in [0,1,2,4]:
            return jsonify({"msg":"No tienes permiso para ver las reseñas de este juego"}), 403
        elif mi_rol == 0 and juego_encontrado.get('activo') == True:
            filtro["visible"] = True
        else:
            estado = request.args.get('estado','activo').lower()
            if estado == 'activo':
                filtro["visible"] = True
            elif estado == 'inactivo':
                filtro["visible"] = False

        pipeline = [
            {"$match": filtro},
            {
                "$lookup": {   
                    "from": "usuarios",
                    "localField": "id_usuario",
                    "foreignField": "_id",
                    "as": "usuario_info"
                }
            },
            {"$unwind": "$usuario_info"},
            {
                "$project": {
                    "_id": 1,
                    "username": "$usuario_info.username",
                    "foto_url": "$usuario_info.foto_url",
                    "comentario": 1,
                    "puntuacion": 1,
                    "recomendado": 1,
                    "fecha_registro": 1,
                    "fecha_modificacion": 1,
                    "visible": 1
                }
            }
        ]

        resennas = list(db.resennas.aggregate(pipeline))

        for resenna in resennas:
            resenna['_id'] = str(resenna['_id'])
            if 'id_juego' in resenna:
                resenna['id_juego'] = str(resenna['id_juego'])
            if 'id_usuario' in resenna:
                resenna['id_usuario'] = str(resenna['id_usuario'])

        return jsonify({"resennas": resennas}), 200

    except Exception as e:
        return jsonify({"msg":"Error al listar las reseñas de juego","error":str(e)}), 500

#listar las reseñas del propio usuario con la foto y el titulo del juego
@application.route('/resenna/listar/mis_resennas',methods=['GET'])
@jwt_required()
def listar_mis_resennas():
    try:
        mi_id = get_jwt_identity()
        
        pipeline = [
            {"$match": {"id_usuario": ObjectId(mi_id)}},
            {
                "$lookup": {   
                    "from": "juegos",
                    "localField": "id_juego",
                    "foreignField": "_id",
                    "as": "juego_info"
                }
            },
            {"$unwind": "$juego_info"},
            {
                "$project": {
                    "_id": 1,
                    "id_juego": 1,
                    "titulo": "$juego_info.titulo",
                    "portada_url": "$juego_info.portada_url",
                    "activo": "$juego_info.activo",
                    "comentario": 1,
                    "puntuacion": 1,
                    "recomendado": 1,
                    "fecha_registro": 1,
                    "fecha_modificacion": 1
                }
            },
            {"$sort": {"fecha_modificacion": -1}}
        ]

        resennas = list(db.resennas.aggregate(pipeline))

        if not resennas:
            return jsonify({"msg":"No tienes reseñas activas"}), 404
        
        for resenna in resennas:
            resenna['_id'] = str(resenna['_id'])
            resenna['id_juego'] = str(resenna['id_juego'])
        return jsonify({"resennas": resennas}), 200
    
    except Exception as e:
        return jsonify({"msg":"Error al listar tus reseñas","error":str(e)}), 500  

#listar las reseñas de un usuario en concreto con la foto y el titulo del juego
@application.route('/resenna/listar/mis_resennas/<string:id_usuario>',methods=['GET'])
@requiere_rol([2,4])
def listar_resennas_usuario(id_usuario):
    try:
        
        pipeline = [
            {"$match": {"id_usuario": ObjectId(id_usuario)}},
            {
                "$lookup": {   
                    "from": "juegos",
                    "localField": "id_juego",
                    "foreignField": "_id",
                    "as": "juego_info"
                }
            },
            {"$unwind": "$juego_info"},
            {
                "$project": {
                    "_id": 1,
                    "id_juego": 1,
                    "titulo": "$juego_info.titulo",
                    "portada_url": "$juego_info.portada_url",
                    "activo": "$juego_info.activo",
                    "comentario": 1,
                    "puntuacion": 1,
                    "recomendado": 1,
                    "fecha_registro": 1,
                    "fecha_modificacion": 1
                }
            },
            {"$sort": {"fecha_modificacion": -1}}
        ]

        resennas = list(db.resennas.aggregate(pipeline))


        for resenna in resennas:
            resenna['_id'] = str(resenna['_id'])
            resenna['id_juego'] = str(resenna['id_juego'])
        return jsonify({"resennas": resennas}), 200
    
    except Exception as e:
        return jsonify({"msg":"Error al listar las reseñas del usuario","error":str(e)}), 500 

if __name__ == '__main__':
    application.run(debug=True, host='0.0.0.0')